# Factory Game

Ein 3D-Spiel in Unity (First-Person): Ein Casino und eine prozedural generierte
Fabrik-Welt in einem Projekt. Man startet direkt in der Fabrik-Welt, sammelt
Ressourcen, stellt Gegenstände her und baut Maschinen auf einem Raster. Per
Taste kann man zwischen Fabrik-Welt und Casino wechseln.

- **Engine:** Unity 6 (`6000.3.9f1`), Universal Render Pipeline (URP)
- **Sprache:** C#
- **Startszene:** `Assets/Scenes/SampleScene.unity`

## Projekt öffnen

1. Unity Hub öffnen -> **Add** -> diesen Ordner auswählen
2. Mit Unity `6000.3.9f1` öffnen (der erste Import dauert einige Minuten,
   der Ordner `Library/` wird automatisch neu erzeugt)
3. `Assets/Scenes/SampleScene.unity` öffnen und auf **Play** drücken

## Steuerung

| Taste | Funktion |
|---|---|
| W A S D / Maus | Bewegen / Umsehen |
| Leertaste / Shift / Strg | Springen / Sprinten / Ducken |
| H | Am Baum oder Erz Material abbauen |
| B | Baumodus an/aus (1-6 oder Mausrad: Maschine wählen, R: drehen, Linksklick: bauen, Rechtsklick: entfernen) |
| C | Crafting-Menü öffnen/schließen |
| 1-0 / Mausrad | Hotbar-Slot wählen |
| Q | Ausgewählten Gegenstand fallen lassen |
| E | Gedroppten Gegenstand in der Nähe aufheben |
| T | Teleport zwischen Casino und Fabrik-Welt |
| F | Gegenstand im Casino verkaufen |

## Inhalt

- **Prozedurale Welt:** Terrain, Biome, Seen und Flüsse, Bäume, Felsen und
  Erzvorkommen (`Assets/Factory/Scripts/MapGenerator.cs`)
- **Bausystem:** Raster-basiertes Platzieren von Maschinen
- **Abbau und Crafting:** Materialien sammeln und zu neuen Gegenständen verarbeiten
- **Hotbar:** Gemeinsames Inventar für Casino- und Fabrik-Gegenstände

## Ordnerstruktur

```
Assets/
  Scripts/      Spieler, Kamera, Hotbar und Casino-Logik
  Factory/      Fabrik-Welt: Generator, Bau-, Abbau- und Crafting-System
    Scripts/    Gameplay-Scripts
    Editor/     Editor-Werkzeuge (Menü "Factory")
  Scenes/       SampleScene = das komplette Spiel (Casino + Fabrik-Welt)
  Casino Building/, Character/, Gui Icons/, Materials/   Modelle, Grafiken, Materialien
docs/           Dokumentation (PDF)
```

## Dokumentation

Ausführliche Beschreibung der Technik und der wichtigsten Code-Stellen:
[`docs/Factory_Game_Dokumentation.pdf`](docs/Factory_Game_Dokumentation.pdf)
