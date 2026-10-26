# WPF 1.3.3: Mehrfachauswahl + freie Position

## Ersetzen
- Models.cs
- SelectionAnalyzer.cs
- ContextAssistController.cs

## Ergänzen
- ContextActionPolicy.cs

## Anwenden
- PopupPlacement-PATCH.txt auf ContextAssistPopup.xaml.cs
- OptionsPage-PATCH.txt auf ContextAssistOptionsPage.cs

## Verhalten
- Einzelne und mehrere Breps, Volumenkörper, Extrusionskörper und Meshes zeigen wieder ein Menü.
- Mehrfachauswahl ausschließlich aus diesen Typen wird als MultipleEnvelopeObjects erkannt.
- Linien-Hüllkörper erscheint nur für SolidBrep, Brep, Mesh und MultipleEnvelopeObjects.
- Neue Positionen: mit 110 px Abstand zur Geometrie oder automatisch in der entferntesten Viewport-Ecke.
