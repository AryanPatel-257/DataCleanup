using System;
using System.IO;
using System.Data;
using System.Text;
using System.Collections.Generic;
using OfficeOpenXml; // Requires EPPlus package installed
using MetadataDataEngine.Models;

namespace MetadataDataEngine.Services
{
    public class OutputGenerator
    {
        public static string InitializeOutputDirectory()
        {
            // Creates a dedicated directory container for this unique run
            string outputFolder = Path.Combine(AppContext.BaseDirectory, "Outputs", $"Run_{DateTime.Now:yyyyMMdd_HHmmss}");
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
            return outputFolder;
        }

        public static void GenerateMigrationSqlScript(string targetDirectory, ClientConfig config, List<DataRow> cleanRows)
        {
            if (cleanRows.Count == 0) return;

            string filePath = Path.Combine(targetDirectory, "success_migration.sql");
            StringBuilder sqlBuilder = new StringBuilder();

            sqlBuilder.AppendLine($"-- Metadata Engine Auto-Generated Data Migration Script");
            sqlBuilder.AppendLine($"-- Client Profile: {config.ClientName}");
            sqlBuilder.AppendLine($"-- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
            sqlBuilder.AppendLine($"USE [{config.TargetDatabase.DestinationTable.Split('.')[0]}]; -- Inferred Database Context");
            sqlBuilder.AppendLine("GO\n");
            sqlBuilder.AppendLine("PRINT 'Initializing secure transaction block...';");
            sqlBuilder.AppendLine("BEGIN TRANSACTION;\n");

            // Build dynamic Column list from config matrix mappings
            List<string> dbColumns = new List<string>();
            foreach (var col in config.Columns)
            {
                dbColumns.Add($"[{col.DatabaseColumnName}]");
            }
            string columnHeaderCsv = string.Join(", ", dbColumns);

            // Generate raw data insert strings dynamically
            sqlBuilder.AppendLine($"INSERT INTO {config.TargetDatabase.DestinationTable} ({columnHeaderCsv}) VALUES");

            for (int i = 0; i < cleanRows.Count; i++)
            {
                List<string> rowValues = new List<string>();
                foreach (var col in config.Columns)
                {
                    string rawVal = cleanRows[i][col.ExcelColumnHeader]?.ToString() ?? string.Empty;

                    // Sanitize currency symbols out of numeric columns if configured
                    if (col.DataType.ToLower() == "decimal")
                    {
                        foreach (var symbol in col.ValidationRules.StripCurrencySymbols)
                        {
                            rawVal = rawVal.Replace(symbol, "");
                        }
                        rowValues.Add(decimal.TryParse(rawVal, out decimal d) ? d.ToString() : "0.00");
                    }
                    else
                    {
                        // Escape single quotes for SQL string safety
                        rowValues.Add($"'{rawVal.Replace("'", "''")}'");
                    }
                }

                string valueLine = $"({string.Join(", ", rowValues)})";
                if (i < cleanRows.Count - 1) valueLine += ",";
                else valueLine += ";";

                sqlBuilder.AppendLine(valueLine);
            }

            sqlBuilder.AppendLine("\nCOMMIT TRANSACTION;");
            sqlBuilder.AppendLine("PRINT 'Data installation completed successfully.';");
            sqlBuilder.AppendLine("GO");

            File.WriteAllText(filePath, sqlBuilder.ToString());
        }

        public static void GenerateTextAuditLogs(string targetDirectory, List<string> successLogs, List<string> errorLogs)
        {
            File.WriteAllLines(Path.Combine(targetDirectory, "success_audit.log"), successLogs);
            File.WriteAllLines(Path.Combine(targetDirectory, "error_quarantine.log"), errorLogs);
        }

        public static void GenerateQuarantineExcelSheet(string targetDirectory, DataTable sourceTable, List<int> failedRowIndices, List<string> failureReasons)
        {
            if (failedRowIndices.Count == 0) return;

            string filePath = Path.Combine(targetDirectory, "quarantine_records.xlsx");

            // [FIXED] Call the explicit setter method required by EPPlus 8+ 
            ExcelPackage.License.SetNonCommercialOrganization("FreelancePortfolio");

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Quarantined Rows");

                // 1. Copy the original column headers structure and append the audit tracker header
                for (int col = 1; col <= sourceTable.Columns.Count; col++)
                {
                    worksheet.Cells[1, col].Value = sourceTable.Columns[col - 1].ColumnName;
                    worksheet.Cells[1, col].Style.Font.Bold = true;
                }
                int engineReasonColIndex = sourceTable.Columns.Count + 1;
                worksheet.Cells[1, engineReasonColIndex].Value = "Engine_Failure_Reason";
                worksheet.Cells[1, engineReasonColIndex].Style.Font.Bold = true;

                // 2. Stream out dirty rows alongside their exact structural error descriptions
                for (int idx = 0; idx < failedRowIndices.Count; idx++)
                {
                    int targetRowExcelIndex = idx + 2;
                    DataRow sourceRow = sourceTable.Rows[failedRowIndices[idx]];

                    for (int col = 1; col <= sourceTable.Columns.Count; col++)
                    {
                        worksheet.Cells[targetRowExcelIndex, col].Value = sourceRow[col - 1];
                    }
                    worksheet.Cells[targetRowExcelIndex, engineReasonColIndex].Value = failureReasons[idx];
                }

                worksheet.Cells.AutoFitColumns();
                package.SaveAs(new FileInfo(filePath));
            }
        }


    }
}
