using ExcelDataReader;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using MetadataDataEngine.Models;
using MetadataDataEngine.Services;

namespace MetadataDataEngine
{
    public partial class Form1 : Form
    {
        // UI Layout Elements
        private Button btnUploadJson;
        private Button btnUploadExcel;
        private Button btnStartPipeline;
        private RichTextBox txtTerminalLog;
        private ProgressBar progressBar;
        private Label lblStatusCounters;

        // State Tracking Properties
        private string selectedJsonPath = string.Empty;
        private string selectedExcelPath = string.Empty;

        public Form1()
        {
            InitializeUserInterfaceComponents();
        }

        private void InitializeUserInterfaceComponents()
        {
            // Main Window Frame Properties
            this.Text = "Enterprise Metadata-Driven Data Quality Engine";
            this.Size = new Size(850, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 243, 246);

            // 1. Upload JSON Configuration Button
            btnUploadJson = new Button
            {
                Text = "1. Load Client JSON Rules",
                Location = new Point(25, 20),
                Size = new Size(200, 38),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnUploadJson.Click += (s, e) => BrowseJsonConfigurationFile();

            // 2. Upload Target Excel Dataset Button
            btnUploadExcel = new Button
            {
                Text = "2. Select Data Excel File",
                Location = new Point(245, 20),
                Size = new Size(200, 38),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnUploadExcel.Click += (s, e) => BrowseExcelDatasetFile();

            // 3. Execution Pipeline Trigger Button
            btnStartPipeline = new Button
            {
                Text = "3. Execute Pipeline",
                Location = new Point(465, 20),
                Size = new Size(180, 38),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 122, 204),
                FlatStyle = FlatStyle.Flat
            };
            btnStartPipeline.Click += async (s, e) => await StartDataPipelineProcessingAsync();

            // 4. Live Colored RichText Log Terminal
            txtTerminalLog = new RichTextBox
            {
                Location = new Point(25, 75),
                Size = new Size(785, 380),
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(0, 255, 0),
                Font = new Font("Consolas", 10, FontStyle.Regular),
                BorderStyle = BorderStyle.None
            };

            // 5. Processing Flow ProgressBar
            progressBar = new ProgressBar
            {
                Location = new Point(25, 475),
                Size = new Size(785, 25),
                Style = ProgressBarStyle.Continuous
            };

            // 6. Running Status and Matrix Counters Label
            lblStatusCounters = new Label
            {
                Text = "System Status: Waiting for client structural configuration mapping...",
                Location = new Point(25, 515),
                Size = new Size(700, 22),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            // Inject the controls array elements directly onto the main single-project frame Canvas
            this.Controls.AddRange(new Control[] {
                btnUploadJson, btnUploadExcel, btnStartPipeline,
                txtTerminalLog, progressBar, lblStatusCounters
            });
        }

        private void BrowseJsonConfigurationFile()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
                openFileDialog.Title = "Select Metadata-Driven Rule Mapping File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedJsonPath = openFileDialog.FileName;

                    try
                    {
                        // Instantiate your new parsing service
                        var parser = new ExcelParsingService();

                        // Parse the JSON file directly into your C# object model
                        ClientConfig config = parser.LoadClientConfigurationMatrix(selectedJsonPath);

                        // Print the verified values to your dark UI terminal screen
                        AppendColoredLogText($"[CONFIG SUCCESS] Loaded metadata blueprint for client: {config.ClientName}\n", Color.Cyan);
                        AppendColoredLogText($"[DATABASE TARGET] Destination Table: {config.TargetDatabase.DestinationTable}\n", Color.Yellow);
                        AppendColoredLogText($"[COLUMNS MAPPED] Monitored rule count: {config.Columns.Count}\n\n", Color.LightGreen);

                        lblStatusCounters.Text = $"Status: {config.ClientName} profile active. Ready for data Excel file mapping.";
                    }
                    catch (Exception ex)
                    {
                        AppendColoredLogText($"[CONFIG ERROR] Failed to parse JSON configuration file. Details: {ex.Message}\n", Color.Red);
                        MessageBox.Show($"JSON Parsing Error: {ex.Message}", "Configuration Fault", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BrowseExcelDatasetFile()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Excel Workbooks (*.xlsx; *.xls; *.csv)|*.xlsx;*.xls;*.csv|All Files (*.*)|*.*";
                openFileDialog.Title = "Select Source Client Dataset Workbook";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedExcelPath = openFileDialog.FileName;
                    AppendColoredLogText($"[DATASET LOADED] Target ingestion target ready: {Path.GetFileName(selectedExcelPath)}\n", Color.Cyan);
                    lblStatusCounters.Text = "Status: Data file verified. Engine ready to execute pipeline calculation.";
                }
            }
        }

        private async Task StartDataPipelineProcessingAsync()
        {
            // Safe Structural Gate Checks
            if (string.IsNullOrEmpty(selectedJsonPath) || string.IsNullOrEmpty(selectedExcelPath))
            {
                MessageBox.Show("Please load both your configuration rules JSON file and a client data Excel dataset workbook before running the pipeline engine execution framework.",
                                "Required Dependencies Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Lock UI interface controls safely during live processing execution loops
            btnUploadJson.Enabled = false;
            btnUploadExcel.Enabled = false;
            btnStartPipeline.Enabled = false;
            txtTerminalLog.Clear();

            AppendColoredLogText("[PIPELINE START] Initializing dataset extraction workers...\n", Color.Cyan);

            try
            {
                // 1. Load active client JSON mapping rules matrix
                var parser = new ExcelParsingService();
                ClientConfig config = parser.LoadClientConfigurationMatrix(selectedJsonPath);

                // 2. Instantiate validation engine
                var validator = new ValidationEngine();
                validator.ResetTrackingStates();

                // Register core text rendering provider fallback safely for ExcelDataReader stream compilation
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                List<DataRow> validRowsCollection = new List<DataRow>();
                List<int> failedRowIndicesCollection = new List<int>();
                List<string> successLogsCollection = new List<string>();
                List<string> errorLogsCollection = new List<string>();
                List<string> failureReasonsCollection = new List<string>();

                // 3. Open Excel file stream memory data structures
                using (var stream = File.Open(selectedExcelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    // Convert spreadsheet rows securely into an in-memory DataSet mapping matrix
                    var resultDataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                    });

                    DataTable dataTable = resultDataSet.Tables[0]; // Targeted first primary table worksheet
                    int totalRows = dataTable.Rows.Count;

                    AppendColoredLogText($"[PROCESS] Discovered data worksheet table containing {totalRows} records.\n\n", Color.Yellow);

                    int successCount = 0;
                    int errorCount = 0;
                    int rowLineCounter = 1; // Tracks physical Excel row line counting for client error logs

                    progressBar.Maximum = totalRows;
                    progressBar.Value = 0;

                    // 4. Iterate through rows dynamically mapped against JSON column tracking configurations
                    foreach (DataRow row in dataTable.Rows)
                    {
                        rowLineCounter++; // Increment index counter loop tracking pointer (Row 1 was headers)
                        bool isEntireRowValid = true;
                        string rowErrorAggregator = string.Empty;
                        // Evaluate each column specified inside the JSON configuration array grid
                        foreach (var colDef in config.Columns)
                        {
                            string headerName = colDef.ExcelColumnHeader;
                            // Confirm column header key matching safely before validation loops
                            if (!dataTable.Columns.Contains(headerName))
                            {
                                isEntireRowValid = false;
                                rowErrorAggregator += $"Target Excel sheet lacks expected column header: '{headerName}'. ";
                                continue;
                            }
                            string rawCellValue = row[headerName]?.ToString() ?? string.Empty;
                            // Pass structural tokens straight into our validation engine
                            if (!validator.ValidateField(rawCellValue, colDef, out string validationFailure))
                            {
                                isEntireRowValid = false;
                                rowErrorAggregator += $"[{headerName} Violation: {validationFailure}] ";
                            }
                        }
                        // 5. Output contextual visualization to the live Dark Log Terminal window screen
                        int currentLoopIndex = dataTable.Rows.IndexOf(row);

                        if (isEntireRowValid)
                        {
                            successCount++;
                            validRowsCollection.Add(row);
                            successLogsCollection.Add($"[{DateTime.Now:HH:mm:ss}] LINE {rowLineCounter:D4}: All columns successfully validated against targets.");
                            AppendColoredLogText($"[LINE {rowLineCounter:D4}] [CLEAN DATA] Row structure successfully verified.\n", Color.LightGreen);
                        }
                        else
                        {
                            errorCount++;
                            failedRowIndicesCollection.Add(currentLoopIndex);
                            failureReasonsCollection.Add(rowErrorAggregator);
                            errorLogsCollection.Add($"[{DateTime.Now:HH:mm:ss}] LINE {rowLineCounter:D4}: Validation Failure: {rowErrorAggregator}");
                            AppendColoredLogText($"[LINE {rowLineCounter:D4}] [QUARANTINE ERR] {rowErrorAggregator}\n", Color.OrangeRed);
                        }
                        // Smoothly update the progress metrics dynamically
                        progressBar.Value = successCount + errorCount;
                        lblStatusCounters.Text = $"Status: Cleaning dataset... | Clean Rows: {successCount} | Failed Rows: {errorCount}";
                        // Auto-scroll terminal canvas downwards continuously
                        txtTerminalLog.SelectionStart = txtTerminalLog.Text.Length;
                        txtTerminalLog.ScrollToCaret();
                        // Yield briefly asynchronously to prevent desktop display frame locking bugs
                        await Task.Delay(15);
                    }
                    // Initialize target directory package structures
                    string runDirectory = OutputGenerator.InitializeOutputDirectory();

                    // Compile outputs concurrently
                    OutputGenerator.GenerateMigrationSqlScript(runDirectory, config, validRowsCollection);
                    OutputGenerator.GenerateTextAuditLogs(runDirectory, successLogsCollection, errorLogsCollection);
                    OutputGenerator.GenerateQuarantineExcelSheet(runDirectory, dataTable, failedRowIndicesCollection, failureReasonsCollection);

                    AppendColoredLogText($"\n🎉 SUCCESS: Data artifacts securely output to: {runDirectory}\n", Color.Cyan);

                    AppendColoredLogText($"\n[EXECUTION COMPLETED] Verified: {successCount} rows | Quarantined: {errorCount} rows.\n", Color.Cyan);
                }
            }
            catch (Exception ex)
            {
                AppendColoredLogText($"\n[FATAL PIPELINE BLOCKED] System error during runtime loop execution processing: {ex.Message}\n", Color.Red);
                MessageBox.Show($"Pipeline execution failure window trace: {ex.Message}", "Processing Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Re-enable dashboard UI access loops securely
                btnUploadJson.Enabled = true;
                btnUploadExcel.Enabled = true;
                btnStartPipeline.Enabled = true;
            }
        }
        private void AppendColoredLogText(string text, Color color)
        {
            txtTerminalLog.SelectionStart = txtTerminalLog.Text.Length;
            txtTerminalLog.SelectionLength = 0;
            txtTerminalLog.SelectionColor = color;
            txtTerminalLog.AppendText(text);
            txtTerminalLog.SelectionColor = txtTerminalLog.ForeColor;
        }
    }
}