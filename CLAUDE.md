# Projektkontext: [Arbeitstitel] – Pen & Paper Rogue-lite

Dieses Dokument fasst den Planungsstand aus einer Design-Diskussion mit Claude (claude.ai) zusammen. Claude Code liest diese Datei automatisch beim Start und kennt damit den Kontext, ohne dass alles neu erklärt werden muss.

## Spielkonzept (Kernidee)

Ein rundenbasiertes Rogue-lite mit echtem Pen & Paper-Gefühl:
- Charakterbogen und Regeln orientieren sich an D&D 5e (sechs Attribute, Rüstungsklasse, Rettungswürfe, Klassen).
- Entscheidungen werden über W20-Würfe + Attributs-Modifikator gegen Rüstungsklasse/Schwierigkeitsgrad aufgelöst – nicht deterministisch.
- Rundenbasierter, taktischer Kampf (Referenz: klassisches D&D / XCOM), kein Echtzeit.
- Rogue-lite-Struktur: prozedural generierte Dungeon-Runs, Meta-Progression zwischen den Runs.
- Ziel: möglichst viele unterschiedliche, synergetische Builds über Klassen, Fähigkeiten und Modifikatoren.

## Kampfsystem: Karten + Würfel

- Aktionen eines Charakters sind an ein **Kartendeck** gebunden (nicht frei wählbare Fähigkeitsliste).
- Hand von 5 Karten pro Zug, gezogen aus einem größeren Deck mit Duplikaten (Sammelgefühl).
- **Regel:** Die Karte bestimmt *was* versucht wird (die Aktion), der Würfelwurf + Stat-Modifikator bestimmt *wie gut* es gelingt. Karten ersetzen nicht den Würfelwurf, sie schränken nur die Auswahl pro Runde ein.
- Nur **eine Karte pro Zug spielbar** (D&D-artige Aktionsökonomie), Rest der Hand wird danach abgelegt.
- Exhaust-Mechanik für "einmal pro Kampf"-Karten (z. B. Heilung) statt Sonderregeln.
- Vorteil/Nachteil (zweimal würfeln, besseres/schlechteres Ergebnis nehmen) ist ein wiederkehrendes Mechanik-Element für Talente/Karten-Synergien.
- **Zwei-Schienen-Modell:**
  1. Aktionsdeck (Kampfkarten, pro Charakter, skaliert mit Stats)
  2. Reliquien/passive Boni (Run- oder dauerhaft, keine Handkarten – zusätzliche Synergie-Ebene ohne mehr Hand-Komplexität)
- Kartenpacks (Meta-Progression) schalten neue Aktionskarten und Reliquien frei. Klassenspezifisch bündeln, Dubletten in Konvertierungs-"Staub" umwandeln.

## Meta-Ebene / Struktur

- **Start:** Ein einzelner Charakter, keine Gruppe, kein Permadeath (bewusst für die erste iterative Phase weggelassen).
- **Später (nächste Phase):** Aufbau einer Gruppe (2-4 Charaktere), Anheuern neuer Abenteurer in einem Hub ("Taverne"), potenziell dauerhafter Tod von Gruppenmitgliedern mit Sicherheitsventilen (z. B. Wiederbelebung, Rückzugsoption, Ruhmeshalle).
- Charaktere/Klassen sind zufällig generiert bei Rekrutierung (Referenz: Battle Brothers-artiges Roster).

## Entwicklungs-Ansatz (iterativ)

1. **Aktuelle Phase:** Kernkampfsystem (Karten + Würfel + Stats) mit einem einzelnen Charakter validieren.
2. Danach: Dungeon-Struktur mit prozeduraler Generierung.
3. Danach: Gruppendynamik, Roster-Management, ggf. Permadeath.
4. Danach: Meta-Progression, Kartenpacks, Freischaltungen.

## Tech-Stack-Entscheidung (Stand: noch nicht final)

- **Plattform-Ziel:** Steam (PC).
- **Spiel ist reines 2D** (Entscheidung gefallen – schnellere Entwicklung, keine 3D-Asset-Erstellung nötig).
- **Engine:** Tendenz zu **Godot** (statt Unity), aus folgenden Gründen:
  - Kein aufwändiger Installationsprozess (portable, keine Account-Pflicht) – wichtig für schnelles Prototyping.
  - Ein-Script-pro-Node-Prinzip erzwingt tendenziell übersichtlichere Struktur als Unitys freies Komponenten-Stacking.
  - Kostenlos, Open Source, kein Lizenzthema bei Steam-Release.
  - Sehr gut für reines 2D geeignet, unterstützt C# (Nutzer kennt C#) oder GDScript.
  - **Noch nicht final entschieden** – sollte vor dem Umstieg vom Browser-Prototyp auf die Engine bestätigt werden.
- **Arbeitsweise:** Claude arbeitet über Claude Code direkt im lokalen Projektordner (volle Dateisystem-Operationen: anlegen, verschieben, löschen, nicht nur Code-Dateien), Git/GitHub dient als Remote/Backup.
- Vorgeschlagene Ordnerstruktur (feature-basiert, engine-agnostisch):
  ```
  /scripts
    /character   (Stats, Charakterbogen-Logik)
    /combat      (Rundenkampf, Würfel-Resolution)
    /cards       (Kartendeck, Handkarten-Logik)
    /dungeon     (Prozedurale Generierung)
    /meta        (Zwischen-Run-Progression)
  /scenes        (jeweils passende Szenen-Dateien)
  ```

## Browser-Prototyp (bereits erstellt)

Ein spielbarer HTML/JS-Prototyp existiert bereits (Datei: `dice-and-cards-prototype.html`), der das Karten+Würfel-Kampfsystem testet:
- Ein Charakter (Krieger, D&D-Stats: STR 16, DEX 12, CON 14, INT 10, WIS 10, CHA 8), AC 15, HP 24.
- Gegner: Höhlengoblin (AC 13, HP 12, Angriffsbonus +4, 1W6+2 Schaden).
- Deck aus 10 Karten (Hieb x4, Wuchtschlag x2, Parade x2, Finte x1, Atem holen x1 [Exhaust]).
- Reines Vanilla HTML/CSS/JS, keine Frameworks – Logik lässt sich konzeptionell auf GDScript/C# übertragen.

**Nächster Schritt:** Diese Datei ins Repo legen, das Kampfsystem am Prototyp weiter testen/verfeinern, dann finale Engine-Entscheidung treffen und die Logik nach Godot (oder gewählter Engine) übertragen.

## Offene Punkte

- Finale Engine-Bestätigung (Godot vs. Alternative).
- Konkrete Klassen über den Krieger hinaus definieren.
- Dungeon-Generierungsalgorithmus (Layout, Encounter-Verteilung).
- Balancing von Karten-Synergien und Progressionskurve.
- Konkretes Reliquien/Bonus-System ausarbeiten.
