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

## Tech-Stack-Entscheidung

- **Plattform-Ziel:** Steam (PC).
- **Spiel ist reines 2D** (Entscheidung gefallen – schnellere Entwicklung, keine 3D-Asset-Erstellung nötig).
- **Zielauflösung: Full HD (1920×1080)** – Fenstergröße in `project.godot` (`[display]`) fest hinterlegt, damit das Spiel von Anfang an in dieser Auflösung startet.
- **Engine: Godot mit C# – Entscheidung gefallen (Stand jetzt).**
  - Sprache: **C#** (Nutzer kennt C# bereits aus Unity, keine Einarbeitung in GDScript nötig).
  - Gründe: kein aufwändiger Installationsprozess (portable, keine Account-Pflicht wie bei Unity Hub) – wichtig für schnellen Einstieg; kostenlos, Open Source, kein Lizenzthema bei Steam-Release; gut für reines 2D geeignet; Ein-Script-pro-Node-Prinzip erzwingt tendenziell übersichtlichere Struktur als Unitys freies Komponenten-Stacking.
  - **Ausstiegsklausel:** Falls sich im Laufe der Entwicklung herausstellt, dass Godot nicht passt, bleibt es beim Godot-Prototyp – das eigentliche Spiel wird dann in Unity **neu aufgesetzt**, nicht aus dem Godot-Stand heraus migriert. Der Godot-Teil ist in diesem Fall Lernprojekt/Wegwerf-Prototyp, kein Unterbau für Unity.
- **Arbeitsweise:** Claude Code arbeitet ausschließlich über Zugriff auf das **Git-Repository** (GitHub), **nicht** auf lokale Ordner/Dateien auf dem Rechner des Nutzers. Alle Änderungen (Code, Szenen, Assets, Doku) erfolgen als Commits/Pushes im Repo. Der Nutzer zieht sich Änderungen selbst lokal (`git pull`), um das Godot-Projekt zu öffnen, zu testen und ggf. eigene Anpassungen vorzunehmen und zurückzupushen.
- Ordnerstruktur (umgesetzt):
  ```
  /scripts
    /character   (Stats, Charakterbogen-Logik: AbilityScores, PlayerCharacter, Enemy)
    /combat      (Rundenkampf, Würfel-Resolution: Dice, CombatEngine, LogTag, AttackResult)
    /cards       (Kartendeck, Handkarten-Logik: CardDefinition + Karten, CardCatalog, Deck)
    /dungeon     (noch leer – nächste Phase)
    /meta        (noch leer – spätere Phase)
  /scenes        (Hub.tscn/Main.tscn + jeweiliges .cs als UI-/Ablauf-Glue, Godot-Konvention: Script liegt bei seiner Szene)
  ```

## Browser-Prototyp (bereits erstellt)

Ein spielbarer HTML/JS-Prototyp existiert bereits (Datei: `dice-and-cards-prototype.html`), der das Karten+Würfel-Kampfsystem testet:
- Ein Charakter (Krieger, D&D-Stats: STR 16, DEX 12, CON 14, INT 10, WIS 10, CHA 8), AC 15, HP 24.
- Gegner: Höhlengoblin (AC 13, HP 12, Angriffsbonus +4, 1W6+2 Schaden).
- Deck aus 10 Karten (Hieb x4, Wuchtschlag x2, Parade x2, Finte x1, Atem holen x1 [Exhaust]).
- Reines Vanilla HTML/CSS/JS, keine Frameworks – Logik lässt sich konzeptionell auf GDScript/C# übertragen.

## Godot-Projekt (bereits aufgesetzt)

Das Godot-4-Projekt liegt im Repo-Root (`project.godot`, `PPRogueLite.csproj`) und enthält die 1:1 aus dem Browser-Prototyp übertragene Kampflogik in C#:
- Gleicher Testkampf wie im Prototyp: Rurik Steinfaust (Krieger) gegen Höhlengoblin, gleiche Stats/AC/HP, gleiches 10-Karten-Deck (Hieb, Wuchtschlag, Parade, Finte, Atem holen).
- `CombatEngine` löst W20-Angriffswürfe/Checks inkl. Vorteil-Mechanik auf, kennt keine UI.
- `scenes/Main.tscn` + `scenes/Main.cs` sind die (bewusst schlicht gehaltene, noch ungestylte) UI: Charakterbogen, Gegner-Panel, Log, Handkarten als Buttons.
- **Noch nicht in der Godot-Editor-Umgebung getestet/geöffnet** – Claude Code hat in dieser Session keinen Zugriff auf Godot/.NET SDK, das Projekt wurde "blind" nach Godot-4-Konventionen angelegt. Erster Test durch den Nutzer lokal steht noch aus.
- Erster Grundtest durch den Nutzer erfolgreich: Kampf läuft in Godot 4.7 (Nutzer hat das Projekt beim Öffnen automatisch migriert) genauso wie im Prototyp.
- Erster visueller Stylingpass umgesetzt: `theme/game_theme.tres` überträgt die Papier-&-Schreibtisch-Farbpalette des Prototyps (dunkler Hintergrund, Papier-Panels, Messing-Buttons, Karten als eigene "CardButton"-Theme-Variante, grün/rote HP-Balken je nach Füllstand). Bewusst ohne die Google-Fonts (Special Elite/Crimson Text) aus dem Prototyp – Systemfont vorerst, Fonts können später ergänzt werden.
- Stylingpass vom Nutzer erfolgreich getestet und für gut befunden.
- Ergebnis-Popup ergänzt (Reveal-Warteschlange): Jede Log-Zeile aus dem Kampf (Würfe, Schaden, Heilung, Systemtexte) erscheint zuerst groß zentriert im `PopupPanel` – Würfe zusätzlich mit Erfolg/Misserfolg-Stempel (TREFFER/VERFEHLT bzw. ERFOLG/FEHLSCHLAG) – und wandert erst danach dauerhaft ins Log. HP-Balken/Werte werden ebenfalls erst nach Abschluss der zugehörigen Popups aktualisiert, statt sofort. Ablauf in `Main.cs` dafür auf `async`/`await` umgestellt (`PlayCard`, `EnemyTurnAsync`, `DrawHandAsync`).
- Popup-Fortschritt läuft über einen "Weiter"-Button statt über einen Timer (`PopupContinueButton`, per `ToSignal` awaited). Der Name der laufenden Aktion (gespielte Karte bzw. Gegner) steht als fester Titel (`PopupActionLabel`) über dem Popup-Inhalt, solange die zugehörigen Zeilen durchgeklickt werden. Die frühere separate "— Du spielst X —"-Log-Zeile wurde entfernt (Karte steht ohnehin im Titel bzw. schon in den Wurf-Zeilen). **Noch nicht in der echten Godot-Umgebung gegengeprüft.**

**Nächster Schritt:** Nutzer testet das Popup/Weiter-Gefühl in Godot und meldet Feinschliff-Wünsche zurück.

## Hub / Menüstruktur (bereits aufgesetzt)

Das Spiel startet jetzt in `scenes/Hub.tscn` (Startszene laut `project.godot`) statt direkt im Kampf – der Hub ist die "Taverne" aus der Meta-Ebene-Planung. Vier Buttons in einem zentrierten Papier-Menüpanel:

- **"Dungeon betreten"** – **funktional**: wechselt per `GetTree().ChangeSceneToFile()` zu `scenes/Main.tscn` und startet damit den Testkampf.
- **"Karten managen"** – **funktional**: wechselt zu `scenes/DeckScreen.tscn` (siehe eigener Abschnitt unten).
- **"Gruppe managen"** – *noch ohne Funktion (UI-Platzhalter)*. Geplant: alle besessenen Charaktere als Charakterkarten; Anklicken einer Karte öffnet das Charakterbogen-Sheet (Bezug zur Roster-/Gruppen-Planung in der Meta-Ebene oben).
- **"Mit Loot entkommen"** – *noch ohne Funktion (UI-Platzhalter)*. Geplant: Weg, einen Run kontrolliert/sicher zu beenden (Loot behalten statt Risiko eines Wipes).

Nach Sieg **oder** Niederlage im Kampf (`Main.cs`, `EndCombat`) zeigt der bisherige Neustart-Button jetzt **"Zurück zum Hub"** und wechselt per Szenenwechsel zurück zu `scenes/Hub.tscn`, statt den Kampf direkt neu zu starten. Jeder erneute Einstieg über "Dungeon betreten" baut den Kampf komplett neu auf (frischer Charakter/Deck/Gegner in `Main._Ready()`).

**Noch nicht in der echten Godot-Umgebung gegengeprüft.**

## Deck-Screen (bereits aufgesetzt)

`scenes/DeckScreen.tscn` (über "Karten managen" im Hub erreichbar) zeigt zwei Spalten in Papier-Panels:
- **Links:** alle Karten im aktuellen Kampf-Deck ("Im Deck (N)").
- **Rechts:** alle besessenen Karten, die *nicht* im Deck sind ("Nicht im Deck (N)").
- Gleiche Kartentypen werden gruppiert mit Stückzahl gezeigt (z. B. "Hieb ×2" im Deck, "Hieb ×3" im Bestand), statt jede physische Karteninstanz einzeln aufzulisten.
- **Drag & Drop:** Karten lassen sich zwischen den beiden Spalten verschieben (eine Karte pro Drag-Vorgang). Ziel wird beim Drop anhand der Mausposition über `DeckScreen._DropData` bestimmt (kein eigener Drop-Zonen-Node) – bewegt jeweils eine Karteninstanz zwischen `PlayerCardCollection.DeckCards`/`BenchCards`.
- **10-Karten-Regel:** Das Deck darf beim Bearbeiten größer oder kleiner als 10 werden, die Anzahl über der Deck-Spalte wird bei mehr als 10 Karten rot. "Zurück zum Hub" ist deaktiviert, solange das Deck **nicht genau 10 Karten** enthält; ein Hinweistext unter den Spalten erklärt warum.
- "Zurück zum Hub" wechselt zurück zu `scenes/Hub.tscn` (nur klickbar, wenn genau 10 Karten im Deck sind).

Datengrundlage ist weiterhin `PPRogueLite.Meta.PlayerCardCollection` (`scripts/meta/PlayerCardCollection.cs`) – eine statische Klasse, deren Felder den Prozess über Szenenwechsel hinweg überleben. Sie hält zwei Listen (`DeckCards`, `BenchCards`), befüllt über `CardCatalog.BuildWarriorStartingDeck()` (10 Karten) und `CardCatalog.BuildWarriorBenchCards()` (7 zusätzliche Karten: 2× Hieb, 2× Wuchtschlag, 1× Parade, 1× Finte, 1× Atem holen).

**Neue wiederverwendbare Kartenansicht `scenes/CardView.tscn`/`CardView.cs`:** zeigt Typ/Name/Beschreibung/Anforderung direkt auf der Karte (keine Hover-Info mehr nötig) und optional eine Stückzahl. Wird sowohl im Deck-Screen (mit `Draggable = true`) als auch für die Handkarten im Kampf (`Main.cs`, mit `Clicked`-Event statt Drag) verwendet – ersetzt die bisherigen einfachen `Button`-Karten. Die alte Theme-Variante `CardButton` (Button-basiert) wurde entfernt und durch `CardPanel` (PanelContainer-basiert) + `CardTypeLabel`/`CardDescriptionLabel`/`CardRequirementLabel` ersetzt.

**Wichtig – noch nicht verbunden:** Der Kampf (`Main.cs`) baut sein Deck weiterhin unabhängig über `CardCatalog.BuildWarriorStartingDeck()` auf und liest **nicht** aus `PlayerCardCollection`. Karten, die im Deck-Screen verschoben werden, wirken sich also noch nicht auf den nächsten Kampf aus – diese Verbindung fehlt noch.

**Noch nicht in der echten Godot-Umgebung gegengeprüft – das gilt besonders für das Drag & Drop** (`_CanDropData`/`_DropData` auf dem Root-Control, verlässt sich auf Godots Ancestor-Bubbling für Drop-Ziele, die selbst nichts überschreiben).

**Nächster Schritt:** Nutzer testet Drag & Drop, Stückzahl-Anzeige und die 10-Karten-Sperre in Godot. Danach mögliche Folgeschritte: `Main.cs` an `PlayerCardCollection` anbinden, damit Deck-Änderungen tatsächlich in den nächsten Kampf übernommen werden; Kartenshop; "Gruppe managen".

## Offene Punkte

- Konkrete Klassen über den Krieger hinaus definieren.
- Dungeon-Generierungsalgorithmus (Layout, Encounter-Verteilung).
- Balancing von Karten-Synergien und Progressionskurve.
- Konkretes Reliquien/Bonus-System ausarbeiten.
