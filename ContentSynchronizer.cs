using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ContentSyncApp
{
    public class ContentSynchronizer
    {
        // Dreamweaver Template Editable Regions
        private readonly List<string> editableRegions = new List<string>
        {
            "head",
            "ScriptHeader", 
            "PageContent",
            "PageEnde"
        };
        
        public SyncResult SyncContent(string sourceFile, string targetFile)
        {
            try
            {
                // Validierung
                if (!File.Exists(sourceFile))
                {
                    return new SyncResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"Quelldatei nicht gefunden: {sourceFile}" 
                    };
                }

                if (!File.Exists(targetFile))
                {
                    return new SyncResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"Zieldatei nicht gefunden: {targetFile}" 
                    };
                }

                // Dateien lesen
                string sourceContent = File.ReadAllText(sourceFile, Encoding.UTF8);
                string targetContent = File.ReadAllText(targetFile, Encoding.UTF8);
                
                int replacedRegions = 0;
                var missingRegions = new List<string>();
                var processedRegions = new List<string>();

                // Synchronisiere alle editierbaren Bereiche
                foreach (var regionName in editableRegions)
                {
                    string extractedContent = ExtractContent(sourceContent, regionName);
                    
                    if (extractedContent != null)
                    {
                        string newTargetContent = ReplaceContent(targetContent, extractedContent, regionName);
                        
                        if (newTargetContent != null)
                        {
                            targetContent = newTargetContent;
                            replacedRegions++;
                            processedRegions.Add(regionName);
                        }
                        else
                        {
                            // Zielbereich existiert nicht
                            missingRegions.Add(regionName);
                        }
                    }
                }

                // Prüfen ob mindestens ein Bereich synchronisiert wurde
                if (replacedRegions == 0)
                {
                    return new SyncResult 
                    { 
                        Success = false, 
                        ErrorMessage = "Keine editierbaren Bereiche konnten synchronisiert werden",
                        MissingRegions = missingRegions
                    };
                }

                // Modifizierten Inhalt speichern
                File.WriteAllText(targetFile, targetContent, Encoding.UTF8);

                return new SyncResult 
                { 
                    Success = true,
                    Message = $"Erfolgreich {replacedRegions} von {editableRegions.Count} Bereichen synchronisiert",
                    BytesWritten = Encoding.UTF8.GetByteCount(targetContent),
                    ProcessedRegions = processedRegions,
                    MissingRegions = missingRegions
                };
            }
            catch (Exception ex)
            {
                return new SyncResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Unerwarteter Fehler: {ex.Message}" 
                };
            }
        }

        private string ExtractContent(string htmlContent, string regionName)
        {
            try
            {
                // Erstelle die spezifischen Marker für diese Region
                string startMarker = $"<!-- InstanceBeginEditable name=\"{regionName}\" -->";
                string endMarker = "<!-- InstanceEndEditable -->";
                
                // Alternative Schreibweisen berücksichtigen
                string[] startVariations = {
                    startMarker,
                    $"<!--InstanceBeginEditable name=\"{regionName}\"-->",
                    $"<!-- InstanceBeginEditable name=\"{regionName}\"-->"
                };
                
                string[] endVariations = {
                    endMarker,
                    "<!--InstanceEndEditable-->"
                };

                int startIndex = -1;
                string usedStartMarker = null;
                
                // Suche nach Start-Marker
                foreach (var marker in startVariations)
                {
                    startIndex = htmlContent.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                    if (startIndex != -1)
                    {
                        usedStartMarker = marker;
                        break;
                    }
                }
                
                if (startIndex == -1)
                    return null;

                startIndex += usedStartMarker.Length;

                // Suche nach End-Marker
                int endIndex = -1;
                foreach (var marker in endVariations)
                {
                    endIndex = htmlContent.IndexOf(marker, startIndex, StringComparison.OrdinalIgnoreCase);
                    if (endIndex != -1)
                        break;
                }
                
                if (endIndex == -1)
                    return null;

                // Extrahiere den Inhalt zwischen den Markierungen
                string content = htmlContent.Substring(startIndex, endIndex - startIndex);
                
                return content;
            }
            catch
            {
                return null;
            }
        }

        private string ReplaceContent(string htmlContent, string newContent, string regionName)
        {
            try
            {
                // Erstelle die spezifischen Marker für diese Region
                string startMarker = $"<!-- InstanceBeginEditable name=\"{regionName}\" -->";
                string endMarker = "<!-- InstanceEndEditable -->";
                
                // Alternative Schreibweisen berücksichtigen
                string[] startVariations = {
                    startMarker,
                    $"<!--InstanceBeginEditable name=\"{regionName}\"-->",
                    $"<!-- InstanceBeginEditable name=\"{regionName}\"-->"
                };
                
                string[] endVariations = {
                    endMarker,
                    "<!--InstanceEndEditable-->"
                };

                int startIndex = -1;
                string usedStartMarker = null;
                
                // Suche nach Start-Marker
                foreach (var marker in startVariations)
                {
                    startIndex = htmlContent.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                    if (startIndex != -1)
                    {
                        usedStartMarker = marker;
                        break;
                    }
                }
                
                if (startIndex == -1)
                    return null;

                int contentStart = startIndex + usedStartMarker.Length;

                // Suche nach End-Marker
                int endIndex = -1;
                foreach (var marker in endVariations)
                {
                    endIndex = htmlContent.IndexOf(marker, contentStart, StringComparison.OrdinalIgnoreCase);
                    if (endIndex != -1)
                        break;
                }
                
                if (endIndex == -1)
                    return null;

                // Erstelle den neuen HTML-Inhalt
                StringBuilder result = new StringBuilder();
                
                // Füge alles vor dem Content-Bereich hinzu
                result.Append(htmlContent.Substring(0, contentStart));
                
                // Füge den neuen Content hinzu
                result.Append(newContent);
                
                // Füge alles nach dem Content-Bereich hinzu
                result.Append(htmlContent.Substring(endIndex));

                return result.ToString();
            }
            catch
            {
                return null;
            }
        }

        public ValidationResult ValidateFile(string filePath)
        {
            var result = new ValidationResult { IsValid = false };

            try
            {
                if (!File.Exists(filePath))
                {
                    result.ErrorMessage = "Datei existiert nicht";
                    return result;
                }

                string content = File.ReadAllText(filePath, Encoding.UTF8);
                result.FoundRegions = new List<string>();

                // Prüfe welche editierbaren Bereiche vorhanden sind
                foreach (var regionName in editableRegions)
                {
                    string extractedContent = ExtractContent(content, regionName);
                    if (extractedContent != null)
                    {
                        result.FoundRegions.Add(regionName);
                        result.ContentLength += extractedContent.Length;
                    }
                }

                if (result.FoundRegions.Count == 0)
                {
                    result.ErrorMessage = "Keine editierbaren Bereiche gefunden";
                    return result;
                }

                result.IsValid = true;
                result.Message = $"{result.FoundRegions.Count} editierbare Bereiche gefunden: {string.Join(", ", result.FoundRegions)}";
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Fehler beim Validieren: {ex.Message}";
                return result;
            }
        }

        public List<string> GetEditableRegions()
        {
            return new List<string>(editableRegions);
        }
    }

    public class SyncResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public int BytesWritten { get; set; }
        public bool ContentUnchanged { get; set; }
        public List<string> ProcessedRegions { get; set; }
        public List<string> MissingRegions { get; set; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public int ContentLength { get; set; }
        public List<string> FoundRegions { get; set; }
    }
}
