using System.Collections.Generic;

namespace MetadataDataEngine.Models
{
    public class ClientConfig
    {
        public string ClientName { get; set; } = string.Empty;
        public DatabaseTarget TargetDatabase { get; set; } = new();
        public GlobalSettings MatrixSettings { get; set; } = new();
        public List<ColumnDefinition> Columns { get; set; } = new();
    }

    public class DatabaseTarget
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DestinationTable { get; set; } = string.Empty;
    }

    public class GlobalSettings
    {
        public string DefaultCurrency { get; set; } = "USD";
        public string DateFormat { get; set; } = "yyyy-MM-dd";
    }

    public class ColumnDefinition
    {
        public string ExcelColumnHeader { get; set; } = string.Empty;
        public string DatabaseColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = "String"; // e.g., Email, Decimal, DateTime, VehicleNumber
        public bool IsRequired { get; set; } = false;
        public ValidationRules ValidationRules { get; set; } = new();
    }

    public class ValidationRules
    {
        public string RegexPattern { get; set; } = string.Empty;
        public bool PreventDuplicates { get; set; } = false;
        public decimal MinValue { get; set; } = 0;
        public List<string> StripCurrencySymbols { get; set; } = new();
    }
}
