# Password Depot Content Synchronizer

## Übersicht
Diese WPF-Anwendung synchronisiert HTML-Inhalte zwischen der deutschen Hauptversion und allen anderen Sprachversionen der Password Depot Website. Die Anwendung kopiert nur die Inhalte zwischen den Dreamweaver Template-Markierungen und lässt den Rest der Datei unverändert.

## Features
- ✅ **Sichere Synchronisation** mit Fehlerprüfung
- ✅ **Backup-Funktion** vor Änderungen
- ✅ **Selektive Sprachauswahl** 
- ✅ **Detaillierte Protokollierung**
- ✅ **Fortschrittsanzeige**
- ✅ **Ausführlicher Statusbericht**
- ✅ **Intelligentes Pfad-Mapping** zwischen Sprachen

## Systemvoraussetzungen
- Windows 10/11
- .NET 8.0 Runtime oder höher
- Schreibrechte im Projektverzeichnis

## Installation & Ausführung

### Option 1: Visual Studio
1. Öffnen Sie die `ContentSyncApp.csproj` in Visual Studio
2. Build → Build Solution (F6)
3. Debug → Start (F5)

### Option 2: Kommandozeile
```bash
cd ContentSyncApp
dotnet build
dotnet run
```

### Option 3: Standalone Build
```bash
dotnet publish -c Release -r win-x64 --self-contained
```
Die ausführbare Datei finden Sie dann in `bin\Release\net8.0-windows\win-x64\publish\`

## Verwendung

### 1. Projektordner auswählen
- Klicken Sie auf "Durchsuchen..." 
- Wählen Sie den Hauptordner mit den Sprachunterordnern (de, en, fr, etc.)

### 2. Analysieren
- Klicken Sie auf "🔍 Analysieren"
- Die App prüft:
  - Vorhandensein der DE-Version
  - Entsprechende Dateien in anderen Sprachen
  - Gültigkeit der Template-Markierungen

### 3. Synchronisieren
- Nach erfolgreicher Analyse wird "🔄 Synchronisieren" aktiviert
- Optional: Deaktivieren Sie einzelne Sprachen
- Klicken Sie auf "Synchronisieren"
- Bestätigen Sie die Sicherheitsabfrage

### 4. Report
- Nach Abschluss wird automatisch ein Report erstellt
- Klicken Sie auf "📊 Report exportieren" zum Öffnen

## Funktionsweise

### Synchronisierte Inhalte
Die App kopiert NUR Inhalte zwischen diesen Markierungen:
```html
<!-- InstanceBeginEditable name="PageContent" -->
    [Dieser Inhalt wird kopiert]
<!-- InstanceEndEditable -->
```

### Pfad-Mapping
Die App übersetzt automatisch deutsche Pfade in die jeweilige Zielsprache:
- `/de/produkt/persoenlich/` → `/en/product/personal-use/`
- `/de/dokumentation/` → `/fr/documentation/`
- `/de/preise/` → `/es/precios/`

### Backup-Strategie
Bei aktivierter Backup-Option:
- Erstellt Ordner: `backup_YYYYMMDD_HHMMSS`
- Kopiert alle Zielsprachen-Ordner
- Vollständige Wiederherstellung möglich

## Fehlerbehebung

### "DE-Ordner nicht gefunden"
- Stellen Sie sicher, dass Sie den richtigen Hauptordner gewählt haben
- Der Ordner muss einen Unterordner "de" enthalten

### "Keine Markierungen gefunden"
- Die HTML-Dateien müssen die Dreamweaver Template-Kommentare enthalten
- Prüfen Sie die Schreibweise der Markierungen

### "Zugriff verweigert"
- Stellen Sie sicher, dass Sie Schreibrechte haben
- Schließen Sie alle HTML-Dateien in Editoren

## Sicherheitshinweise
⚠️ **WICHTIG**: 
- Die App überschreibt Inhalte in den Zieldateien
- Erstellen Sie IMMER ein Backup (standardmäßig aktiviert)
- Testen Sie zuerst mit einer kleinen Auswahl
- Prüfen Sie die Ergebnisse stichprobenartig

## Report-Format
Der generierte Report enthält:
- Zeitstempel der Synchronisation
- Anzahl verarbeiteter Dateien pro Sprache
- Erfolgs-/Fehlerstatistiken
- Detaillierte Fehlerliste (falls vorhanden)
- Backup-Pfad (falls erstellt)

## Unterstützte Sprachen
- CS - Tschechisch
- DA - Dänisch  
- EL - Griechisch
- EN - Englisch
- ES - Spanisch
- FI - Finnisch
- FR - Französisch
- HU - Ungarisch
- IT - Italienisch
- NL - Niederländisch
- NO - Norwegisch
- PL - Polnisch
- PT - Portugiesisch
- SV - Schwedisch

## Lizenz
© 2024 AceBIT GmbH - Internes Tool für Password Depot Website-Management

## Support
Bei Fragen oder Problemen wenden Sie sich an:
- Stelios (Website-Team)
- IT-Support AceBIT

## Changelog
### Version 2.0.0 (November 2024)
- **NEU**: Synchronisation von 4 editierbaren Bereichen:
  - `head` - Meta-Tags, CSS, SEO-Informationen
  - `ScriptHeader` - JavaScript im Header-Bereich
  - `PageContent` - Hauptinhalt der Seite
  - `PageEnde` - Scripts am Seitenende
- Verbesserte Fehlerbehandlung
- Detailliertes Logging pro Bereich
- Threading-Optimierungen

### Version 1.0.0 (November 2024)
- Initiale Version
- Grundfunktionen implementiert
- 15 Sprachen unterstützt
- Backup-Funktion
- Detaillierte Fehlerbehandlung
