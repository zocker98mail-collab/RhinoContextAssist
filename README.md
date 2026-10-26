# WPF Build-Fix 1.3.1

## Behobene Fehler
- `UseWindowsForms` entfernt. Dadurch verschwinden die globalen Namenskonflikte mit Eto und WPF.
- WPF `Button` wird explizit über Alias `WpfButton` verwendet.
- Controller verwendet explizit `Eto.Drawing.PointF`.
- WPF-XAML erzeugt eine öffentliche partielle Klasse; Code-behind ist jetzt ebenfalls `public partial`.
- Methoden mit internen Parametertypen sind jetzt `internal`, wodurch die Accessibility-Fehler entfallen.
- Mehrmonitor-Arbeitsbereich wird ohne WinForms über Win32 `MonitorFromPoint/GetMonitorInfo` ermittelt.
- Rhino wird über das native Owner-HWND verbunden.

## Installation
Diese drei Dateien ersetzen:
- RhinoContextAssist.csproj
- ContextAssistController.cs
- ContextAssistPopup.xaml.cs

`ContextAssistPopup.xaml` bleibt aus dem vorherigen WPF-Paket erhalten.
