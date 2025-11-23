using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Diagnostics;

namespace ContentSyncApp
{
    public partial class MainWindow : Window
    {
        private readonly ContentSynchronizer synchronizer;
        private readonly FileMapping fileMapper;
        private bool _isProcessing = false;
        private DateTime startTime;
        private System.Windows.Threading.DispatcherTimer timer;

        public bool IsProcessing 
        { 
            get => _isProcessing; 
            set 
            { 
                _isProcessing = value;
                progressBar.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            synchronizer = new ContentSynchronizer();
            fileMapper = new FileMapping();
            InitializeTimer();
            
            // Setze "Alle" CheckBox nach der Initialisierung
            chkAll.IsChecked = true;
        }

        private void InitializeTimer()
        {
            timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += (s, e) =>
            {
                if (_isProcessing)
                {
                    var elapsed = DateTime.Now - startTime;
                    txtTime.Text = $"Laufzeit: {elapsed:mm\\:ss}";
                }
            };
            timer.Interval = TimeSpan.FromSeconds(1);
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            // Windows Forms FolderBrowserDialog verwenden
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Wählen Sie den Password Depot Projektordner";
                dialog.ShowNewFolderButton = false;
                
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtProjectPath.Text = dialog.SelectedPath;
                    ValidateProjectPath();
                }
            }
        }

        private bool ValidateProjectPath()
        {
            if (string.IsNullOrWhiteSpace(txtProjectPath.Text))
            {
                LogError("Kein Projektpfad angegeben!");
                return false;
            }

            if (!Directory.Exists(txtProjectPath.Text))
            {
                LogError($"Verzeichnis nicht gefunden: {txtProjectPath.Text}");
                return false;
            }

            var dePath = Path.Combine(txtProjectPath.Text, "de");
            if (!Directory.Exists(dePath))
            {
                LogError($"DE-Ordner nicht gefunden: {dePath}");
                return false;
            }

            LogSuccess($"✓ Projektordner validiert: {txtProjectPath.Text}");
            return true;
        }

        private async void BtnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateProjectPath()) return;

            ClearLog();
            SetProcessingState(true);
            btnSync.IsEnabled = false;

            // UI-Werte VOR dem Task.Run abrufen (im UI-Thread)
            var projectPath = txtProjectPath.Text;
            var languages = GetSelectedLanguages();

            await Task.Run(() =>
            {
                try
                {
                    Dispatcher.Invoke(() => LogInfo("=== ANALYSE GESTARTET ==="));

                    var deFiles = GetFilteredHtmlFiles(Path.Combine(projectPath, "de"));

                    Dispatcher.Invoke(() =>
                    {
                        txtDeFiles.Text = deFiles.Count.ToString();
                        LogInfo($"Gefundene DE-Dateien: {deFiles.Count} (ausgeschlossene Verzeichnisse: intern, _temp, _backup, .git, .svn)");
                    });

                    int totalToSync = 0;
                    var missingFiles = new List<string>();

                    foreach (var lang in languages)
                    {
                        Dispatcher.Invoke(() => LogInfo($"\nAnalysiere {lang.ToUpper()}..."));
                        
                        foreach (var deFile in deFiles)
                        {
                            var targetFile = fileMapper.GetTargetPath(deFile, projectPath, lang);
                            
                            if (!File.Exists(targetFile))
                            {
                                missingFiles.Add($"{lang}: {Path.GetFileName(targetFile)}");
                            }
                            else
                            {
                                totalToSync++;
                            }
                        }
                    }

                    Dispatcher.Invoke(() =>
                    {
                        txtToSync.Text = totalToSync.ToString();
                        
                        if (missingFiles.Count > 0)
                        {
                            LogWarning($"\n⚠ {missingFiles.Count} Dateien haben kein Gegenstück:");
                            foreach (var missing in missingFiles.Take(10))
                            {
                                LogWarning($"  - {missing}");
                            }
                            if (missingFiles.Count > 10)
                            {
                                LogWarning($"  ... und {missingFiles.Count - 10} weitere");
                            }
                        }

                        LogSuccess($"\n✓ Analyse abgeschlossen: {totalToSync} Dateien können synchronisiert werden");
                        btnSync.IsEnabled = totalToSync > 0;
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => LogError($"Fehler bei der Analyse: {ex.Message}"));
                }
                finally
                {
                    Dispatcher.Invoke(() => SetProcessingState(false));
                }
            });
        }

        private async void BtnSync_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateProjectPath()) return;

            var result = MessageBox.Show(
                "Möchten Sie wirklich die Synchronisation starten?\n\n" +
                "Dies wird die Inhalte in den Zielsprachen überschreiben!",
                "Synchronisation bestätigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            ClearLog();
            SetProcessingState(true);
            btnSync.IsEnabled = false;
            btnAnalyze.IsEnabled = false;

            // UI-Werte VOR dem Task.Run abrufen (im UI-Thread)
            var projectPath = txtProjectPath.Text;
            var languages = GetSelectedLanguages();
            var createBackup = chkBackup.IsChecked == true;

            var report = new StringBuilder();
            report.AppendLine($"=== SYNCHRONISATIONSBERICHT ===");
            report.AppendLine($"Datum: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Projektpfad: {projectPath}");
            report.AppendLine();

            await Task.Run(() =>
            {
                try
                {
                    var deFiles = GetFilteredHtmlFiles(Path.Combine(projectPath, "de"));

                    int successCount = 0;
                    int errorCount = 0;
                    var errors = new List<string>();

                    // Backup erstellen wenn gewünscht
                    if (createBackup)
                    {
                        Dispatcher.Invoke(() => LogInfo("Erstelle Backup..."));
                        CreateBackup(projectPath, languages);
                    }

                    int totalOperations = deFiles.Count * languages.Count;
                    int currentOperation = 0;

                    foreach (var lang in languages)
                    {
                        Dispatcher.Invoke(() => LogInfo($"\n=== Synchronisiere {lang.ToUpper()} ==="));
                        int langSuccess = 0;
                        int langError = 0;

                        foreach (var deFile in deFiles)
                        {
                            currentOperation++;
                            var progress = (currentOperation * 100) / totalOperations;
                            Dispatcher.Invoke(() => progressBar.Value = progress);

                            try
                            {
                                var targetFile = fileMapper.GetTargetPath(deFile, projectPath, lang);
                                
                                if (File.Exists(targetFile))
                                {
                                    var result = synchronizer.SyncContent(deFile, targetFile);
                                    
                                    if (result.Success)
                                    {
                                        successCount++;
                                        langSuccess++;
                                        
                                        // Erstelle detaillierte Statusmeldung
                                        string statusMsg = $"✓ {Path.GetFileName(targetFile)}";
                                        if (result.ProcessedRegions != null && result.ProcessedRegions.Count > 0)
                                        {
                                            statusMsg += $" [{string.Join(", ", result.ProcessedRegions)}]";
                                        }
                                        
                                        Dispatcher.Invoke(() => 
                                        {
                                            LogSuccess(statusMsg);
                                            txtSuccess.Text = successCount.ToString();
                                        });
                                    }
                                    else
                                    {
                                        errorCount++;
                                        langError++;
                                        errors.Add($"{lang}/{Path.GetFileName(targetFile)}: {result.ErrorMessage}");
                                        Dispatcher.Invoke(() => 
                                        {
                                            LogError($"✗ {Path.GetFileName(targetFile)}: {result.ErrorMessage}");
                                            txtErrors.Text = errorCount.ToString();
                                        });
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                langError++;
                                errors.Add($"{lang}: {ex.Message}");
                                Dispatcher.Invoke(() => 
                                {
                                    LogError($"✗ Fehler: {ex.Message}");
                                    txtErrors.Text = errorCount.ToString();
                                });
                            }
                        }

                        report.AppendLine($"{lang.ToUpper()}: {langSuccess} erfolgreich, {langError} Fehler");
                    }

                    // Report generieren
                    report.AppendLine();
                    report.AppendLine($"=== ZUSAMMENFASSUNG ===");
                    report.AppendLine($"Gesamt erfolgreich: {successCount}");
                    report.AppendLine($"Gesamt Fehler: {errorCount}");
                    
                    if (errors.Count > 0)
                    {
                        report.AppendLine();
                        report.AppendLine("=== FEHLERDETAILS ===");
                        foreach (var error in errors)
                        {
                            report.AppendLine(error);
                        }
                    }

                    // Report speichern
                    var reportPath = Path.Combine(projectPath, $"sync_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                    File.WriteAllText(reportPath, report.ToString());

                    Dispatcher.Invoke(() =>
                    {
                        LogInfo($"\n{new string('=', 50)}");
                        LogSuccess($"✓ Synchronisation abgeschlossen!");
                        LogInfo($"Erfolgreich: {successCount} | Fehler: {errorCount}");
                        LogInfo($"Report gespeichert: {reportPath}");
                        btnExport.IsEnabled = true;
                        btnExport.Tag = reportPath;
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => LogError($"Kritischer Fehler: {ex.Message}"));
                }
                finally
                {
                    Dispatcher.Invoke(() => 
                    {
                        SetProcessingState(false);
                        btnAnalyze.IsEnabled = true;
                    });
                }
            });
        }

        private void CreateBackup(string projectPath, List<string> languages)
        {
            var backupPath = Path.Combine(projectPath, $"backup_{DateTime.Now:yyyyMMdd_HHmmss}");
            Directory.CreateDirectory(backupPath);

            foreach (var lang in languages)
            {
                var sourcePath = Path.Combine(projectPath, lang);
                if (Directory.Exists(sourcePath))
                {
                    var targetPath = Path.Combine(backupPath, lang);
                    CopyDirectory(sourcePath, targetPath);
                }
            }

            Dispatcher.Invoke(() => LogSuccess($"✓ Backup erstellt: {backupPath}"));
        }

        private void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
            }
        }

        private List<string> GetFilteredHtmlFiles(string rootPath)
        {
            var filteredFiles = new List<string>();
            CollectHtmlFilesRecursive(rootPath, filteredFiles);
            return filteredFiles;
        }

        private void CollectHtmlFilesRecursive(string currentPath, List<string> fileList)
        {
            try
            {
                // Prüfe ob aktuelles Verzeichnis ausgeschlossen werden soll
                var dirName = Path.GetFileName(currentPath);
                if (!string.IsNullOrEmpty(dirName) && fileMapper.IsDirectoryExcluded(dirName))
                {
                    return;
                }

                // Sammle HTM-Dateien im aktuellen Verzeichnis (nicht ausgeschlossene)
                foreach (var file in Directory.GetFiles(currentPath, "*.htm"))
                {
                    var fileName = Path.GetFileName(file);
                    if (!fileMapper.IsFileExcluded(fileName))
                    {
                        fileList.Add(file);
                    }
                }

                // Rekursiv in Unterverzeichnisse
                foreach (var subDir in Directory.GetDirectories(currentPath))
                {
                    CollectHtmlFilesRecursive(subDir, fileList);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Überspringe Verzeichnisse ohne Zugriff
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => LogWarning($"Warnung beim Durchsuchen von {currentPath}: {ex.Message}"));
            }
        }

        private List<string> GetSelectedLanguages()
        {
            var languages = new List<string>();
            
            // Diese Methode wird nur vom UI-Thread aufgerufen
            if (chkCS != null && chkCS.IsChecked == true) languages.Add("cs");
            if (chkDA != null && chkDA.IsChecked == true) languages.Add("da");
            if (chkEL != null && chkEL.IsChecked == true) languages.Add("el");
            if (chkEN != null && chkEN.IsChecked == true) languages.Add("en");
            if (chkES != null && chkES.IsChecked == true) languages.Add("es");
            if (chkFI != null && chkFI.IsChecked == true) languages.Add("fi");
            if (chkFR != null && chkFR.IsChecked == true) languages.Add("fr");
            if (chkHU != null && chkHU.IsChecked == true) languages.Add("hu");
            if (chkIT != null && chkIT.IsChecked == true) languages.Add("it");
            if (chkNL != null && chkNL.IsChecked == true) languages.Add("nl");
            if (chkNO != null && chkNO.IsChecked == true) languages.Add("no");
            if (chkPL != null && chkPL.IsChecked == true) languages.Add("pl");
            if (chkPT != null && chkPT.IsChecked == true) languages.Add("pt");
            if (chkSV != null && chkSV.IsChecked == true) languages.Add("sv");

            return languages;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Implementierung für Abbruch
            _isProcessing = false;
            SetProcessingState(false);
            LogWarning("Vorgang abgebrochen!");
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (btnExport.Tag != null)
            {
                var reportPath = btnExport.Tag.ToString();
                if (File.Exists(reportPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = reportPath,
                        UseShellExecute = true
                    });
                }
            }
        }

        private void ChkAll_Checked(object sender, RoutedEventArgs e)
        {
            // Verhindere Ausführung während der Initialisierung
            if (!IsInitialized) return;
            SetAllLanguages(true);
        }

        private void ChkAll_Unchecked(object sender, RoutedEventArgs e)
        {
            // Verhindere Ausführung während der Initialisierung
            if (!IsInitialized) return;
            SetAllLanguages(false);
        }

        private void SetAllLanguages(bool isChecked)
        {
            // Null-Check für alle CheckBoxen während der Initialisierung
            if (chkCS != null) chkCS.IsChecked = isChecked;
            if (chkDA != null) chkDA.IsChecked = isChecked;
            if (chkEL != null) chkEL.IsChecked = isChecked;
            if (chkEN != null) chkEN.IsChecked = isChecked;
            if (chkES != null) chkES.IsChecked = isChecked;
            if (chkFI != null) chkFI.IsChecked = isChecked;
            if (chkFR != null) chkFR.IsChecked = isChecked;
            if (chkHU != null) chkHU.IsChecked = isChecked;
            if (chkIT != null) chkIT.IsChecked = isChecked;
            if (chkNL != null) chkNL.IsChecked = isChecked;
            if (chkNO != null) chkNO.IsChecked = isChecked;
            if (chkPL != null) chkPL.IsChecked = isChecked;
            if (chkPT != null) chkPT.IsChecked = isChecked;
            if (chkSV != null) chkSV.IsChecked = isChecked;
        }

        private void SetProcessingState(bool processing)
        {
            _isProcessing = processing;
            progressBar.Visibility = processing ? Visibility.Visible : Visibility.Collapsed;
            btnCancel.IsEnabled = processing;
            txtStatus.Text = processing ? "Verarbeitung läuft..." : "Bereit";
            
            if (processing)
            {
                startTime = DateTime.Now;
                timer.Start();
            }
            else
            {
                timer.Stop();
            }
        }

        private void ClearLog()
        {
            rtbLog.Document.Blocks.Clear();
            txtSuccess.Text = "0";
            txtErrors.Text = "0";
        }

        private void LogInfo(string message)
        {
            AppendLog(message, Colors.Black);
        }

        private void LogSuccess(string message)
        {
            AppendLog(message, Colors.Green);
        }

        private void LogWarning(string message)
        {
            AppendLog(message, Colors.Orange);
        }

        private void LogError(string message)
        {
            AppendLog(message, Colors.Red);
        }

        private void AppendLog(string message, Color color)
        {
            var paragraph = new Paragraph(new Run(message))
            {
                Foreground = new SolidColorBrush(color),
                Margin = new Thickness(0, 2, 0, 2)
            };
            
            rtbLog.Document.Blocks.Add(paragraph);
            rtbLog.ScrollToEnd();
        }
    }
}
