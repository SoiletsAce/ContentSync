using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ContentSyncApp
{
    public class FileMapping
    {
        // Mapping-Tabellen für Verzeichnisnamen DE -> andere Sprachen (Fallback)
        private readonly Dictionary<string, Dictionary<string, string>> directoryMappings;

        public FileMapping()
        {
            directoryMappings = new Dictionary<string, Dictionary<string, string>>();
            InitializeMappings();
        }

        /// <summary>
        /// Liest die hreflang-Links aus einer HTML-Datei und gibt die Zuordnungen zurück
        /// </summary>
        public Dictionary<string, string> ExtractLanguageLinks(string htmlFilePath)
        {
            var languageLinks = new Dictionary<string, string>();

            try
            {
                string content = File.ReadAllText(htmlFilePath, Encoding.UTF8);

                // Regex für <link rel="alternate" hreflang="xx" href="...">
                // Unterstützt verschiedene Varianten (mit/ohne Leerzeichen, Reihenfolge, etc.)
                var patterns = new[]
                {
                    @"<link\s+rel=[""']alternate[""']\s+hreflang=[""']([a-z]{2})[""']\s+href=[""']([^""']+)[""']\s*/?>",
                    @"<link\s+hreflang=[""']([a-z]{2})[""']\s+rel=[""']alternate[""']\s+href=[""']([^""']+)[""']\s*/?>",
                    @"<link\s+hreflang=[""']([a-z]{2})[""']\s+href=[""']([^""']+)[""']\s+rel=[""']alternate[""']\s*/?>",
                    @"<link\s+href=[""']([^""']+)[""']\s+rel=[""']alternate[""']\s+hreflang=[""']([a-z]{2})[""']\s*/?>",
                    @"<link\s+href=[""']([^""']+)[""']\s+hreflang=[""']([a-z]{2})[""']\s+rel=[""']alternate[""']\s*/?>",
                    @"<link\s+rel=[""']alternate[""']\s+href=[""']([^""']+)[""']\s+hreflang=[""']([a-z]{2})[""']\s*/?>"
                };

                foreach (var pattern in patterns)
                {
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                    var matches = regex.Matches(content);

                    foreach (Match match in matches)
                    {
                        string lang, href;

                        // Je nach Pattern-Reihenfolge sind die Gruppen unterschiedlich
                        if (pattern.IndexOf("hreflang") < pattern.IndexOf("href"))
                        {
                            lang = match.Groups[1].Value;
                            href = match.Groups[2].Value;
                        }
                        else
                        {
                            href = match.Groups[1].Value;
                            lang = match.Groups[2].Value;
                        }

                        if (!languageLinks.ContainsKey(lang))
                        {
                            languageLinks[lang] = href;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Bei Fehler leeres Dictionary zurückgeben
                Console.WriteLine($"Fehler beim Extrahieren der Language-Links aus {htmlFilePath}: {ex.Message}");
            }

            return languageLinks;
        }

        /// <summary>
        /// Konvertiert eine URL aus hreflang in einen lokalen Dateipfad
        /// Beispiel: https://www.password-depot.de/cs/download/download-server.htm
        ///        -> projectRoot/cs/download/download-server.htm
        /// </summary>
        public string UrlToFilePath(string url, string projectRoot, string language)
        {
            try
            {
                // Schritt 1: Entferne Domain falls vorhanden
                // https://www.password-depot.de/cs/download/download-server.htm
                // -> /cs/download/download-server.htm
                if (url.StartsWith("http://") || url.StartsWith("https://"))
                {
                    var uri = new Uri(url);
                    url = uri.AbsolutePath;
                }

                // Schritt 2: Entferne führenden Slash
                // /cs/download/download-server.htm -> cs/download/download-server.htm
                url = url.TrimStart('/');

                // Schritt 3: Konvertiere zu Windows-Pfad (falls nötig)
                url = url.Replace('/', Path.DirectorySeparatorChar);

                // Schritt 4: Kombiniere direkt mit Projektpfad
                // projectRoot + cs/download/download-server.htm
                // = projectRoot/cs/download/download-server.htm
                var filePath = Path.Combine(projectRoot, url);

                return filePath;
            }
            catch
            {
                return null;
            }
        }

        private void InitializeMappings()
        {
            // Englisch Mappings
            directoryMappings["en"] = new Dictionary<string, string>
            {
                { "dokumentation", "documentation" },
                { "produkt", "product" },
                { "persoenlich", "personal-use" },
                { "geschaeftlich", "business-use" },
                { "preise", "pricing" },
                { "unternehmen", "company" },
                { "ressourcen", "resources" },
                { "download", "download" },
                { "intern", "internal" },
                { "offerte", "quote" }
            };

            // Französisch Mappings
            directoryMappings["fr"] = new Dictionary<string, string>
            {
                { "dokumentation", "documentation" },
                { "produkt", "produit" },
                { "persoenlich", "utilisation-personnelle" },
                { "geschaeftlich", "utilisation-professionnelle" },
                { "preise", "tarification" },
                { "unternehmen", "societe" },
                { "ressourcen", "ressources" },
                { "download", "telecharger" },
                { "offerte", "offre" }
            };

            // Spanisch Mappings
            directoryMappings["es"] = new Dictionary<string, string>
            {
                { "dokumentation", "documentacion" },
                { "produkt", "producto" },
                { "persoenlich", "uso-personal" },
                { "geschaeftlich", "uso-comercial" },
                { "preise", "precios" },
                { "unternehmen", "empresa" },
                { "ressourcen", "recursos" },
                { "download", "descargar" },
                { "offerte", "oferta" }
            };

            // Italienisch Mappings
            directoryMappings["it"] = new Dictionary<string, string>
            {
                { "dokumentation", "documentation" },
                { "produkt", "product" },
                { "persoenlich", "personal-use" },
                { "geschaeftlich", "business-use" },
                { "preise", "pricing" },
                { "unternehmen", "company" },
                { "ressourcen", "resources" },
                { "download", "download" },
                { "offerte", "quote" }
            };

            // Niederländisch Mappings
            directoryMappings["nl"] = new Dictionary<string, string>
            {
                { "dokumentation", "documentatie" },
                { "produkt", "product" },
                { "persoenlich", "persoonlijk-gebruik" },
                { "geschaeftlich", "zakelijk-gebruik" },
                { "preise", "prijzen" },
                { "unternehmen", "bedrijf" },
                { "ressourcen", "bronnen" },
                { "download", "download" },
                { "offerte", "offerte" },
                { "support", "ondersteuning" }
            };

            // Für andere Sprachen verwende die gleiche Struktur wie EN (Standard)
            foreach (var lang in new[] { "cs", "da", "el", "fi", "hu", "no", "pl", "pt", "sv" })
            {
                if (!directoryMappings.ContainsKey(lang))
                {
                    directoryMappings[lang] = new Dictionary<string, string>
                    {
                        { "dokumentation", "documentation" },
                        { "produkt", "product" },
                        { "persoenlich", "personal-use" },
                        { "geschaeftlich", "business-use" },
                        { "preise", "pricing" },
                        { "unternehmen", "company" },
                        { "ressourcen", "resources" },
                        { "download", "download" },
                        { "offerte", "quote" }
                    };
                }
            }
        }

        public string GetTargetPath(string deFilePath, string projectRoot, string targetLanguage)
        {
            string targetPath;
            string source;
            return GetTargetPath(deFilePath, projectRoot, targetLanguage, out targetPath, out source) ? targetPath : null;
        }

        /// <summary>
        /// Erweiterte Version die auch die Quelle des Mappings zurückgibt
        /// </summary>
        public bool GetTargetPath(string deFilePath, string projectRoot, string targetLanguage, out string targetPath, out string mappingSource)
        {
            targetPath = null;
            mappingSource = "unknown";

            try
            {
                // PRIMÄR: Versuche zuerst, die Zuordnung aus den hreflang-Links zu lesen
                var languageLinks = ExtractLanguageLinks(deFilePath);

                if (languageLinks.ContainsKey(targetLanguage))
                {
                    var url = languageLinks[targetLanguage];
                    var path = UrlToFilePath(url, projectRoot, targetLanguage);

                    if (path != null)
                    {
                        targetPath = path;
                        mappingSource = "hreflang";
                        return true;
                    }
                }

                // FALLBACK: Verwende das alte manuelle Mapping
                // Relativen Pfad vom DE-Ordner extrahieren
                var dePath = Path.Combine(projectRoot, "de");
                var relativePath = GetRelativePath(dePath, deFilePath);

                // Pfad-Komponenten aufteilen
                var pathParts = relativePath.Split(Path.DirectorySeparatorChar);

                // Dateiname mapping
                var fileName = MapFileName(Path.GetFileName(deFilePath), targetLanguage);

                // Verzeichnispfad mapping
                var mappedParts = new List<string>();
                for (int i = 0; i < pathParts.Length - 1; i++)
                {
                    mappedParts.Add(MapDirectoryName(pathParts[i], targetLanguage));
                }
                mappedParts.Add(fileName);

                // Zielpfad zusammensetzen
                var targetRelativePath = string.Join(Path.DirectorySeparatorChar.ToString(), mappedParts);
                targetPath = Path.Combine(projectRoot, targetLanguage, targetRelativePath);
                mappingSource = "fallback";

                return true;
            }
            catch (Exception ex)
            {
                mappingSource = $"error: {ex.Message}";
                return false;
            }
        }

        private string MapDirectoryName(string deName, string targetLanguage)
        {
            if (!directoryMappings.ContainsKey(targetLanguage))
                return deName;

            var mapping = directoryMappings[targetLanguage];
            
            if (mapping.ContainsKey(deName.ToLower()))
                return mapping[deName.ToLower()];
            
            return deName;
        }

        private string MapFileName(string deFileName, string targetLanguage)
        {
            // Spezielle Dateinamen-Mappings
            var fileNameMappings = new Dictionary<string, Dictionary<string, string>>
            {
                ["en"] = new Dictionary<string, string>
                {
                    { "onlinehilfen.htm", "onlinehelp.htm" },
                    { "dokumentation.htm", "documentation.htm" },
                    { "systemanforderungen.htm", "systemreq.htm" },
                    { "deinstallation.htm", "uninstall.htm" },
                    { "deinstallieren.htm", "uninstalling.htm" },
                    { "bestellen.htm", "order.htm" },
                    { "bestellen-server.htm", "order-server.htm" },
                    { "danke.htm", "thankyou.htm" },
                    { "impressum.htm", "imprint.htm" },
                    { "ueber.htm", "about.htm" },
                    { "datenschutz.htm", "privacy.htm" },
                    { "agb.htm", "tos.htm" },
                    { "bilder.htm", "screenshots.htm" },
                    { "vorherige.htm", "previous.htm" },
                    { "was-ist-neu.htm", "whats-new.htm" }
                }
            };

            // Tour-spezifische Mappings für EN
            if (targetLanguage == "en" && deFileName.Contains("tour"))
            {
                var tourMappings = new Dictionary<string, string>
                {
                    { "authentifizierung.htm", "authentication.htm" },
                    { "topleiste.htm", "topbar.htm" },
                    { "passwort-automatisch-ausfuellen-addon.htm", "auto-fill-web-forms-using-add-ons.htm" },
                    { "passwort-datei-erstellen.htm", "creating-a-new-password-file.htm" },
                    { "passwoerter-mobile-geraete.htm", "transferring-passwords-to-mobile-devices.htm" },
                    { "passwort-automatisch-ausfuellen.htm", "auto-completion-via-button.htm" },
                    { "passwort-generator.htm", "password-generator.htm" },
                    { "ordner-in-password-depot-anlegen.htm", "creating-folders.htm" },
                    { "sicherungskopien-erstellen.htm", "creating-backups.htm" },
                    { "verschluesselte-anhaenge.htm", "encrypted-attachments.htm" },
                    { "passworter-aus-browser-hinzufuegen.htm", "adding-passwords-from-browser.htm" },
                    { "sicherungskopien-oeffnen.htm", "opening-backups.htm" },
                    { "passwort-hinzufuegen.htm", "adding-password-entries.htm" }
                };

                if (tourMappings.ContainsKey(deFileName))
                    return tourMappings[deFileName];
            }

            // Standard-Mappings anwenden
            if (fileNameMappings.ContainsKey(targetLanguage) && 
                fileNameMappings[targetLanguage].ContainsKey(deFileName))
            {
                return fileNameMappings[targetLanguage][deFileName];
            }

            // Für andere Sprachen: Versuche generisches Mapping
            if (targetLanguage != "de")
            {
                // Einfache Wort-für-Wort Übersetzung für gemeinsame Begriffe
                var genericMappings = new Dictionary<string, string>
                {
                    { "passwort", "password" },
                    { "kennwort", "password" },
                    { "kennwoerter", "passwords" },
                    { "sicherheit", "security" },
                    { "herunterladen", "download" },
                    { "hilfe", "help" }
                };

                var mappedName = deFileName;
                foreach (var mapping in genericMappings)
                {
                    mappedName = mappedName.Replace(mapping.Key, mapping.Value);
                }

                // Wenn sich der Name geändert hat, gib ihn zurück
                if (mappedName != deFileName)
                    return mappedName;
            }

            // Standardfall: Behalte den deutschen Dateinamen
            return deFileName;
        }

        private string GetRelativePath(string basePath, string fullPath)
        {
            if (fullPath.StartsWith(basePath))
            {
                var relativePath = fullPath.Substring(basePath.Length);
                if (relativePath.StartsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    relativePath = relativePath.Substring(1);
                }
                return relativePath;
            }
            
            throw new ArgumentException($"Path {fullPath} is not under {basePath}");
        }

        public bool IsFileExcluded(string fileName)
        {
            // Liste von Dateien, die nicht synchronisiert werden sollen
            var excludedFiles = new HashSet<string>
            {
                ".htaccess",
                ".htpasswd",
                "web.config",
                "robots.txt",
                "sitemap.xml"
            };

            // Prüfe ob Datei ausgeschlossen werden soll
            return excludedFiles.Contains(fileName.ToLower());
        }

        public bool IsDirectoryExcluded(string dirName)
        {
            // Liste von Verzeichnissen, die nicht synchronisiert werden sollen
            var excludedDirs = new HashSet<string>
            {
                "intern",
                "internal",
                "_temp",
                "_backup",
                ".git",
                ".svn"
            };

            return excludedDirs.Contains(dirName.ToLower());
        }
    }
}
