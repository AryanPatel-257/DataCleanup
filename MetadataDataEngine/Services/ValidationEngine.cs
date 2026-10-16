using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MetadataDataEngine.Models;

namespace MetadataDataEngine.Services
{
    public class ValidationEngine
    {
        // Tracks encountered unique hashes to catch duplicates across the dataset rows
        private readonly Dictionary<string, HashSet<string>> _duplicateTrackers = new();

        public void ResetTrackingStates()
        {
            _duplicateTrackers.Clear();
        }

        public bool ValidateField(string rawValue, ColumnDefinition columnConfig, out string failureReason)
        {
            failureReason = string.Empty;
            string cleanValue = rawValue?.Trim() ?? string.Empty;

            // 1. Mandatory Input Inflow Check
            if (columnConfig.IsRequired && string.IsNullOrWhiteSpace(cleanValue))
            {
                failureReason = $"Column '{columnConfig.ExcelColumnHeader}' is marked as mandatory but was empty.";
                return false;
            }

            // Skip further validations if field is optional and blank
            if (!columnConfig.IsRequired && string.IsNullOrWhiteSpace(cleanValue))
            {
                return true;
            }

            // 2. Data Type Formatting and Contextual Validations
            switch (columnConfig.DataType.ToLower())
            {
                case "email":
                    string emailRegex = !string.IsNullOrEmpty(columnConfig.ValidationRules.RegexPattern)
                        ? columnConfig.ValidationRules.RegexPattern
                        : @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

                    if (!Regex.IsMatch(cleanValue, emailRegex, RegexOptions.IgnoreCase))
                    {
                        failureReason = $"Malformed structural email format: '{cleanValue}'";
                        return false;
                    }
                    cleanValue = cleanValue.ToLower(); // Standardize formatting
                    break;

                case "decimal":
                    // Strip defined currency formatting symbols dynamically from the string
                    string trackingValue = cleanValue;
                    if (columnConfig.ValidationRules.StripCurrencySymbols != null)
                    {
                        foreach (var symbol in columnConfig.ValidationRules.StripCurrencySymbols)
                        {
                            trackingValue = trackingValue.Replace(symbol, "");
                        }
                    }

                    if (!decimal.TryParse(trackingValue, out decimal numericValue))
                    {
                        failureReason = $"Value '{cleanValue}' could not be compiled into a valid decimal point digit.";
                        return false;
                    }

                    if (numericValue < columnConfig.ValidationRules.MinValue)
                    {
                        failureReason = $"Numeric constraint out of range. Minimum required: {columnConfig.ValidationRules.MinValue}, Value parsed: {numericValue}";
                        return false;
                    }
                    break;

                case "datetime":
                    // Fall back to standard format strings if not customized in settings
                    if (!DateTime.TryParse(cleanValue, out _))
                    {
                        failureReason = $"Date string '{cleanValue}' does not conform to standard parsing formats.";
                        return false;
                    }
                    break;

                default:
                    // If a custom Regex pattern is specified for plain strings (e.g., Vehicle Numbers)
                    if (!string.IsNullOrEmpty(columnConfig.ValidationRules.RegexPattern))
                    {
                        if (!Regex.IsMatch(cleanValue, columnConfig.ValidationRules.RegexPattern))
                        {
                            failureReason = $"Value '{cleanValue}' failed custom structural validation regex pattern match.";
                            return false;
                        }
                    }
                    break;
            }

            // 3. De-duplication Rule Processing Layer
            if (columnConfig.ValidationRules.PreventDuplicates)
            {
                string targetKey = columnConfig.DatabaseColumnName;
                if (!_duplicateTrackers.ContainsKey(targetKey))
                {
                    _duplicateTrackers[targetKey] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                if (_duplicateTrackers[targetKey].Contains(cleanValue))
                {
                    failureReason = $"Duplicate constraint violation. Data entry '{cleanValue}' already logged in history columns.";
                    return false;
                }

                _duplicateTrackers[targetKey].Add(cleanValue);
            }

            return true;
        }
    }
}
