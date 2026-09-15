# PowerShellUI

Ein kleines Windows-Desktop-Programm (WPF/.NET 8), das PowerShell-Skripte aus einer lokalen
Bibliothek einliest, deren Parameter automatisch als UI-Formular darstellt und die Ausführung
per Knopfdruck vereinfacht. Zielgruppe sind vor allem Skripte für Windows/Active Directory,
Azure, Microsoft Entra, Exchange Online und PnP PowerShell.

## Download

Fertige, eigenständige `.exe` (self-contained, kein separat installiertes .NET nötig) unter
[Releases](../../releases) — als ZIP mit `PowerShellUI.exe` + Beispiel-Skriptbibliothek.
Ein neues Release entsteht automatisch, sobald ein Tag im Format `vX.Y.Z` gepusht wird
(`.github/workflows/release.yml`), oder lässt sich manuell über den Actions-Tab
("Release" → "Run workflow") auslösen.

## Funktionsumfang

- **Skript-Bibliothek**: rekursiv durchsuchbarer Ordner mit `.ps1`-Dateien; der oberste
  Unterordner (z. B. `Azure`, `Entra`, `ExchangeOnline`, `PnP`, `ActiveDirectory`) wird als
  Kategorie in der UI verwendet.
- **Automatische Formular-Generierung**: der `param()`-Block jedes Skripts wird per
  `System.Management.Automation.Language.Parser` analysiert (Name, Typ, Pflichtfeld,
  `ValidateSet`-Werte, `HelpMessage`) und daraus ein Eingabeformular gebaut (Textfeld,
  Checkbox für `switch`, Dropdown für `ValidateSet`).
- **PowerShell 5.1 und 7 parallel**: ein Skript kann per `#Requires -PSEdition Core` bzw.
  `-PSEdition Desktop` eine Edition erzwingen. Ohne Vorgabe wählt die App automatisch
  (bevorzugt PowerShell 7) oder der Benutzer wählt manuell im Dropdown.
- **Modul-Check & Installation per Knopfdruck**: `#Requires -Modules ...` wird aus jedem
  Skript gelesen, der Installationsstatus je Modul angezeigt und fehlende Module (z. B.
  `PnP.PowerShell`, `ExchangeOnlineManagement`, `Microsoft.Graph.Groups`, `Az.Resources`,
  `ActiveDirectory`) können über `Install-Module -Scope CurrentUser` installiert werden.
- **Sichere Ausführung**: Parameterwerte werden als einzelne Prozessargumente an
  `powershell.exe`/`pwsh.exe -File ...` übergeben (keine String-Konkatenation), die
  Live-Ausgabe (stdout/stderr) wird im UI gestreamt.

## Projektstruktur

```
PowerShellUI.sln
src/
  PowerShellUI.Core/     Wiederverwendbare Logik (kein UI-Framework-Bezug)
    Models/               ScriptInfo, ScriptParameterInfo, RequiredModuleInfo, ...
    Services/             Host-Erkennung, Skript-Introspektion, Modulverwaltung, Ausführung
    Assets/               Eingebettete Hilfsskripte (PS 5.1- und 7-kompatibel)
  PowerShellUI.App/      WPF-Anwendung (net8.0-windows)
    ViewModels/           MainViewModel + Parameter-/Modul-ViewModels (hand-rolled MVVM)
    MainWindow.xaml       UI: Skriptliste, Parameterformular, Modulstatus, Ausgabe-Log
scripts/                Beispiel-Skriptbibliothek (Vorlagen zum Anpassen)
  ActiveDirectory/
  Azure/
  Entra/
  ExchangeOnline/
  PnP/
```

Die Beispielbibliothek unter `scripts/` wird beim Build automatisch nach
`bin/.../ScriptLibrary` kopiert, damit die App direkt nach dem ersten Start etwas anzeigt.
Für den produktiven Einsatz über "Durchsuchen..." auf die eigene (ggf. freigegebene)
Skript-Bibliothek umstellen.

## Eigene Skripte hinzufügen

Damit ein Skript korrekt als Formular dargestellt wird:

```powershell
#Requires -Modules PnP.PowerShell
#Requires -PSEdition Core   # optional, nur wenn eine bestimmte Edition nötig ist

<#
.SYNOPSIS
    Kurzbeschreibung, die in der UI als Untertitel angezeigt wird.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, HelpMessage = "Tooltip-Text im Formular")]
    [string]$SiteUrl,

    [Parameter(HelpMessage = "Wird als Dropdown dargestellt")]
    [ValidateSet('Klein', 'Mittel', 'Groß')]
    [string]$Groesse,

    [switch]$Interaktiv   # wird als Checkbox dargestellt
)
```

Einfach als `.ps1`-Datei in den passenden Kategorie-Unterordner der Bibliothek legen —
kein zusätzlicher Registrierungsschritt nötig.

## Build & Ausführung (Windows, .NET 8 SDK erforderlich)

```powershell
dotnet build .\PowerShellUI.sln
dotnet run --project .\src\PowerShellUI.App
```

Die WPF-App läuft ausschließlich unter Windows (WPF-Abhängigkeit sowie `powershell.exe`/
`pwsh.exe`-Host-Erkennung). `PowerShellUI.Core` selbst ist plattformneutral aufgebaut
(reines `net8.0`, keine WPF-Referenz).

## Bekannte Einschränkungen / Ausbaustufen

- Nur einfache Parametertypen werden im UI abgebildet (String/Zahl als Textfeld, `switch`
  als Checkbox, `ValidateSet` als Dropdown). Arrays, Hashtables oder komplexe Objekttypen
  werden aktuell als Textfeld dargestellt (Rohwert wird 1:1 als Parameterwert übergeben).
- Authentifizierung/Credential-Handling wird bewusst den Skripten selbst überlassen
  (`Connect-AzAccount`, `Connect-MgGraph`, `Connect-ExchangeOnline`, `Connect-PnPOnline` —
  jeweils interaktiv oder per hinterlegtem Zertifikat/App-Registrierung im Skript selbst).
- Keine automatisierten Tests/CI bisher vorhanden.
