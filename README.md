# Rhino Context Assist 2.0.1 Search Window Build-Fix

## Ersetzen

Im Hybrid-HUD-Projekt ausschließlich diese Datei ersetzen:

- `CommandSearchWindow.xaml.cs`

`CommandSearchWindow.xaml` bleibt unverändert.

## Ursache

Die WPF-Klasse muss wegen XAML öffentlich bleiben. Der bisherige öffentliche Konstruktor verwendete aber den internen Typ `ContextAction`. Dadurch war der Konstruktor sichtbarer als sein Parametertyp.

## Korrektur

Der Konstruktor ist jetzt `internal`:

```csharp
internal CommandSearchWindow(
    IEnumerable<ContextAction> actions,
    Action<ContextAction> execute)
```

Damit stimmen die Sichtbarkeiten überein.
