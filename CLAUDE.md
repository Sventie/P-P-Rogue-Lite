# Projektkontext: [Arbeitstitel] – Pen & Paper Rogue-lite

Dieses Dokument fasst den Planungsstand aus einer Design-Diskussion mit Claude (claude.ai) zusammen. Claude Code liest diese Datei automatisch beim Start und kennt damit den Kontext, ohne dass alles neu erklärt werden muss.

## Spielkonzept (Kernidee)

Ein Rogue-lite mit Pen & Paper-Fundament, aber **Echtzeit-Kampf** (Entscheidung vom 2026-08-02, siehe "Pivot" unten):
- Charakterbogen und Regeln orientieren sich an D&D 5e (sechs Attribute, Rüstungsklasse, Rettungswürfe, Klassen).
- Entscheidungen werden über W20-Würfe + Attributs-Modifikator gegen Rüstungsklasse/Schwierigkeitsgrad aufgelöst – nicht deterministisch. **Der Würfelwurf selbst ist nicht mehr sichtbar/interaktiv** (kein Popup mehr) – er läuft verdeckt im Hintergrund, sichtbar wird nur das Ergebnis (Flugtext: Schaden, "Verfehlt", Krit-Farbe).
- **Echtzeit-Kampf** (Referenz: Vampire Survivors) statt rundenbasiert: Spieler bewegt sich per WASD, Fähigkeiten lösen automatisch auf Cooldown aus. Mechanischer Skill des Spielers kommt über Bewegung/Positionierung (Gegnern ausweichen, Reichweite ausnutzen), nicht über Karten-Klicks.
- Rogue-lite-Struktur: prozedural generierte Dungeon-Runs, Meta-Progression zwischen den Runs.
- Ziel: möglichst viele unterschiedliche, synergetische Builds über Klassen, Fähigkeiten und Modifikatoren.

### Pivot: rundenbasiert → Echtzeit (wichtig für den Kontext)

Ursprünglich war der Kampf rundenbasiert (Karte klicken → Würfel-Popup → Ergebnis, siehe Browser-Prototyp unten). Nutzer wollte mehr mechanischen Spieler-Skill statt "durch Textboxen klicken" – nach Diskussion (Vampire-Survivors-Referenz, aber mit sichtbarem Erfolg/Misserfolg-Feedback) wurde entschieden:
- **Bewegung/Kampf:** Echtzeit-Arena statt Runden. Angriffe/Fähigkeiten lösen **vollautomatisch** aus (kein manuelles Auslösen) – Skill kommt aus Bewegung/Positionierung, nicht aus Timing-Klicks.
- **Karten bleiben zentral**, ändern aber ihre Rolle: keine Hand-von-5-pro-Zug mehr, sondern **Level-up-Ziehungen**. Die Karten-/Deck-Thematik (inkl. Deck-Screen) bleibt dadurch visuell/mechanisch relevant, nur die Auslösung ändert sich.
- Der alte rundenbasierte Kampf (`scenes/Main.tscn`, `CombatEngine`, Popup-System) ist **vollständig abgelöst** und wird von keinem Menü mehr erreicht (siehe "Altlasten" unten) – bewusst nicht gelöscht, falls doch etwas daraus gebraucht wird.

## Kampfsystem: Echtzeit-Arena mit Karten als Fähigkeiten

- Aktionen eines Charakters sind weiterhin an ein **Kartendeck** gebunden – aber Karten werden nicht mehr pro Zug gespielt, sondern beim **Level-up** aus dem Deck gezogen und dauerhaft als aktive Fähigkeit ausgerüstet, die von da an selbstständig auf Cooldown auslöst.
- **Regel bleibt im Kern gleich:** Die Karte bestimmt *was* die Fähigkeit tut, ein verdeckter W20-Wurf + Stat-Modifikator bestimmt *wie gut* es gelingt (Treffer/Schaden/Erfolg) – nur ohne Popup, das Ergebnis zeigt sich als Flugtext.
- Start jedes Runs: automatisch mit "Hieb" ausgerüstet (garantiert einen Basisangriff), damit kein Run ohne Angriffsmöglichkeit startet, selbst wenn das erste Level-up zufällig eine defensive Karte zieht.
- Exhaust-Mechanik entfällt (kein Konzept von "verbrauchen" mehr bei Dauerfähigkeiten); Vorteil/Nachteil (zweimal würfeln, besseres Ergebnis) bleibt als Mechanik erhalten (z. B. Finte gewährt Vorteil auf den nächsten Treffer).
- **Zwei-Schienen-Modell (Aktionsdeck + Reliquien/passive Boni) und Kartenpacks/Meta-Progression bleiben als Zielbild bestehen**, nur die Auslösung im Kampf hat sich geändert (siehe Pivot oben).

## Meta-Ebene / Struktur

- **Start:** Ein einzelner Charakter, keine Gruppe, kein Permadeath (bewusst für die erste iterative Phase weggelassen).
- **Später (nächste Phase):** Aufbau einer Gruppe (2-4 Charaktere), Anheuern neuer Abenteurer in einem Hub ("Taverne"), potenziell dauerhafter Tod von Gruppenmitgliedern mit Sicherheitsventilen (z. B. Wiederbelebung, Rückzugsoption, Ruhmeshalle).
- Charaktere/Klassen sind zufällig generiert bei Rekrutierung (Referenz: Battle Brothers-artiges Roster).

## Entwicklungs-Ansatz (iterativ)

1. **Aktuelle Phase:** Kern-Echtzeit-Kampf (Bewegung + Karten-als-Fähigkeiten + verdeckte Würfel) mit einem einzelnen Charakter validieren.
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
- **Neue Issues nur nach Rückfrage:** Kommen im Gespräch Themen für spätere Änderungen auf – eine vom Nutzer erwähnte Erweiterung, die noch nicht jetzt umgesetzt werden soll, oder ein eigener Vorschlag von Claude Code –, wird dafür ein neues GitHub-Issue **vorgeschlagen**, aber nicht automatisch angelegt. Claude Code fragt aktiv nach, ob das Issue angelegt werden soll, damit der Nutzer die Kontrolle darüber behält, was tatsächlich im Backlog landet (siehe "Offene Punkte / nächste Schritte" unten für die aktuelle Liste).
- Ordnerstruktur (umgesetzt):
  ```
  /scripts
    /character   (Stats, Charakterbogen-Logik: AbilityScores, PlayerCharacter, Enemy [Kampfwerte-Container, von scenes/Enemy.cs pro Instanz befüllt])
    /combat      (Würfel-Resolution: Dice; CombatEngine/LogTag/AttackResult/RollStamp/AbilityCheckResult gehören zum alten rundenbasierten Kampf, siehe Altlasten)
    /cards       (Kartendeck-Logik: CardDefinition + Karten, CardCatalog, Deck – jetzt Grundlage der Echtzeit-Fähigkeiten)
    /dungeon     (StageNode/DungeonMap/DungeonMapGenerator – verzweigter Pfad-Graph eines Dungeons, kein Godot-Bezug)
    /enemies     (EnemyDefinition + 5 Gegnertypen, BossDefinition + Oger-Häuptling, EnemyCatalog – Gegner-Vielfalt der Echtzeit-Arena, kein Godot-Bezug)
    /meta        (PlayerCardCollection/PlayerWallet/DungeonRun – Kartenbesitz/Gold/laufender Dungeon-Fortschritt über Szenenwechsel/Runs hinweg)
  /scenes        (Hub/Arena/Player/Enemy/Projectile/FloatingText/DeckScreen/CardView/Camp/Main + jeweiliges .cs als UI-/Ablauf-Glue, Godot-Konvention: Script liegt bei seiner Szene)
  ```
- Godot 4.7, `Godot.NET.Sdk` (.NET), `<Nullable>enable</Nullable>` in `PPRogueLite.csproj`. Fenster fest auf 1920×1080 in `project.godot` (`[display]`), Startszene `res://scenes/Hub.tscn` (`[application] run/main_scene`), globales Theme über `[gui] theme/custom`. Kein Autoload/Singleton-Mechanismus – Cross-Scene-State läuft ausschließlich über C#-`static`-Felder (siehe `PlayerCardCollection`).

## Technischer Aufbau (für neue Sessions: hier zuerst lesen)

Kompakte Referenz, damit eine neue Session ohne Gesprächsverlauf schnell versteht, wie der Code zusammenhängt – ergänzt die Ordnerstruktur oben um Verantwortlichkeiten und gelernte Godot-Fallstricke.

**Namespaces** (alle unter `PPRogueLite`):
- `PPRogueLite` (Root): Godot-Node-Scripts direkt unter `scenes/` – `Hub`, `Arena`, `Player`, `Enemy`, `Projectile`, `FloatingText`, `CardView`, `DeckScreen`, `DeckStackView`, `Camp`, `Main` (Altlast).
- `PPRogueLite.Character`: `AbilityScores` (Ability-Enum + Modifier-Berechnung), `PlayerCharacter`, `Enemy` (Kampfwerte-Container, nicht zu verwechseln mit dem gleichnamigen Godot-Node `scenes/Enemy.cs`!) – reine Datenklassen, kein Godot-Bezug.
- `PPRogueLite.Combat`: `Dice` (statischer W20/WN-Roller, einziger RNG im Projekt) + Altlasten `CombatEngine`/`LogTag`/`AttackResult`/`AbilityCheckResult`/`RollStamp`.
- `PPRogueLite.Cards`: `CardDefinition` (abstract: Id/DisplayName/CardType/Description/RequirementText/IsExhaust/`Kind`/`CoupledCardId`/`Play(CombatEngine)`) + 8 konkrete Karten (5 Aktions-, 3 Modifikatorkarten, siehe Modifikatorkarten-Abschnitt unten), `CardCatalog` (baut Start-/Bench-Deck, `AllCardTypes()`), `Deck` (Draw-/Discard-Pile, Shuffle, optional ohne Mischen via `shuffleOnCreate: false` – kein Godot-Bezug).
- `PPRogueLite.Enemies`: `EnemyDefinition` (abstract: Id/DisplayName/Movement/OnHitEffect/Stats/MoveSpeed/EngagementRange/AttackCooldown/Radius/MinStage) + 5 konkrete Gegnertypen, `EnemyCatalog` (`AllTypes`, `AvailableForStage(stage)`) – kein Godot-Bezug, siehe Gegner-Abschnitt unten (Issue #9). Dazu `BossDefinition` (abstract, erbt von `EnemyDefinition`: zusätzlich Summon-/Slam-Cooldowns + Slam-Geometrie/-Schaden, `Movement` fest auf `EnemyMovement.Boss`) + `OgerHaeuptlingDefinition` als erste Bossklasse – siehe Boss-Kampf-Abschnitt unten (Issue #11). Bewusst **nicht** in `EnemyCatalog.AllTypes`, da Bosse nicht Teil des zufälligen Wellen-Spawn-Pools sind.
- `PPRogueLite.Meta`: `PlayerCardCollection` (static class, `DeckCards`/`BenchCards`-Listen), `PlayerWallet` (static class, `Gold`), `DungeonRun` (static class, Fortschritt eines laufenden Dungeons über Stages hinweg inkl. des generierten Pfad-Graphs – siehe Dungeon-Struktur-Abschnitt unten).
- `PPRogueLite.Dungeon`: `StageNode` (Id/Column/WaveCount/GoldMultiplier/NextNodeIds), `DungeonMap` (Knotenliste + `GetNode(id)`), `DungeonMapGenerator` (baut den verzweigten Pfad, siehe Dungeon-Struktur-Abschnitt unten) – reine Datenklassen, kein Godot-Bezug.

**Wichtig zu verstehen – `CardDefinition.Play(CombatEngine)` ist Altlast:** Das war der Auslöse-Mechanismus des alten rundenbasierten Kampfs. Die Echtzeit-Arena nutzt ihn **nicht** – `Player.TriggerAbility` verdrahtet das Verhalten jeder Karte stattdessen fest per `switch` über `CardDefinition.Id` (siehe Echtzeit-Arena-Abschnitt unten). Beim Ergänzen neuer Karten also nicht `Play()` implementieren in der Annahme, das reiche – der Switch in `Player.cs` muss den neuen Id-Fall auch behandeln.

**Gleiches Muster bei Gegnern:** `scenes/Enemy.cs` ist der EINE Godot-Node für alle Gegnertypen (Issue #9) – er verzweigt Bewegung/Angriff per `switch` über `EnemyDefinition.Movement` (`Enemy.UpdateMelee`/`UpdateRanged`/`UpdateHitAndRun`), nicht über je ein eigenes Script pro Typ. Neue Gegnertypen brauchen also i. d. R. nur eine neue `EnemyDefinition`-Unterklasse in `scripts/enemies/` + einen Eintrag in `EnemyCatalog.AllTypes` + ggf. einen neuen `EnemyMovement`/`EnemyOnHitEffect`-Fall, falls sich Bewegungsmuster/Treffer-Effekt grundlegend unterscheiden.

**Datenfluss Kartenbesitz → Run:** `PlayerCardCollection` ist eine statische Klasse (überlebt Szenenwechsel via CLR-Static-Feld-Lebensdauer, kein Autoload nötig). `DeckScreen` liest/schreibt sie direkt (×/+-Buttons verschieben Karteninstanzen zwischen `DeckCards`/`BenchCards`). `Player._Ready()` kopiert beim **ersten** Arena-Start eines Dungeons den aktuellen Stand von `DeckCards` in ein **laufeigenes** `new Deck(...)` – Deck-Änderungen wirken sich also erst auf den **nächsten** Dungeon aus, nie auf einen laufenden.

**Datenfluss Charakter-Fortschritt → nächste Stage:** Godot behält beim Szenenwechsel keinen Node-State (die alte `Player`-Instanz wird beim Verlassen der Arena zerstört). Damit HP/Level/XP/ausgerüstete Fähigkeiten/Nachziehstapel über mehrere Stages *desselben* Dungeons erhalten bleiben, schreibt `Player.SaveProgress()` sie vor jedem Szenenwechsel in `DungeonRun` (aufgerufen von `Arena.CompleteStage()`); `Player._Ready()` liest sie beim Start der nächsten Stage über `DungeonRun.HasProgress` wieder ein, statt einen frischen Charakter zu bauen. Siehe Dungeon-Struktur-Abschnitt unten für den vollen Ablauf.

**Szenen/Node-Struktur** (Script liegt immer neben seiner `.tscn`, Godot-Konvention):
- `Hub.tscn` (Startszene, "Taverne"): `Control` mit 4 Buttons in einem Papier-Panel + Gold-Anzeige; `Hub.cs` verdrahtet nur die ersten beiden (`GetTree().ChangeSceneToFile("res://scenes/Arena.tscn"|"res://scenes/DeckScreen.tscn")`, "Dungeon betreten" ruft zusätzlich `DungeonRun.Start()`).
- `Arena.tscn`: `Node2D`-Root (`Arena.cs` = Orchestrator) mit Kind-Node `Player` (Instanz von `Player.tscn`), `WaveTransitionTimer`, und `HUD` (`CanvasLayer`) mit `MarginContainer → VBoxContainer` (AbilityBar, HP/XP-Labels+Bars, StageLabel, WaveLabel, SurvivalLabel, OutcomeLabel, ContinueButton) plus separatem `LevelUpLayer` (`MarginContainer`, initial leer, wird zur Laufzeit von `Arena.cs` mit dem 3-Spalten-Level-up-Screen befüllt und wieder geleert).
- `Player.tscn`: `Node2D`, Script `Player.cs` (WASD-Bewegung, Fähigkeiten-Liste, XP/Level, kein Sprite – `_Draw()`).
- `Enemy.tscn`: `Node2D`, Script `Enemy.cs` (gemeinsamer Node für alle 5 Gegnertypen, datengetrieben über `EnemyDefinition`, siehe Gegner-Abschnitt unten und "Gleiches Muster bei Gegnern" oben).
- `Projectile.cs`: reiner Code, keine eigene `.tscn` nötig (wie `DeckStackView.cs`) – Fernkampf-Projektil, wird per `new Projectile { ... }` + `AddChild()` erzeugt statt über eine `PackedScene`.
- `FloatingText.tscn`: `Node2D` mit `Label`-Kind, Script `FloatingText.cs` (Tween-Animation, self-destruct via `QueueFree`).
- `DeckScreen.tscn`: zwei Spalten (Im Deck / Nicht im Deck), je ein `VBoxContainer` [HeaderLabel, PanelContainer→ScrollContainer→GridContainer] mit `CardView`-Instanzen.
- `CardView.tscn`: wiederverwendbare Kartenansicht (Panel + Labels + ActionButton), genutzt in `DeckScreen` UND im Arena-Level-up-Screen.
- `Camp.tscn` ("Lager"): `Control`, gleiche Papier-Menü-Struktur wie `Hub.tscn` (Titel/Untertitel/Gold-Anzeige + zentriertes Menüpanel), zeigt zur Laufzeit erzeugte Routen-Buttons (`ChoiceContainer`, siehe Dungeon-Struktur-Abschnitt unten) plus einen festen "Dungeon beenden"-Button. Zwischenstopp zwischen zwei Stages *innerhalb* eines Dungeons – nicht zu verwechseln mit dem Hub/der Taverne, die nur *zwischen* zwei Dungeons erreichbar ist.
- `Main.tscn`/`Main.cs`: Altlast des alten rundenbasierten Kampfs, von keinem Menü mehr erreicht – siehe "Altlasten" unten, nicht ohne Rücksprache löschen.

**Godot-C#-Muster/Learnings** (gegen echte Bugs erarbeitet – beim Weiterbauen beachten):
- **Node-Lifecycle:** `_Ready()` feuert erst NACH `AddChild()` in den lebenden SceneTree, nicht direkt bei `PackedScene.Instantiate()`. Reihenfolge beim dynamischen Erzeugen immer: `Instantiate()` → Properties setzen → `AddChild()` → erst dann Methoden wie `Populate()` aufrufen, die auf in `_Ready()` gesetzte Felder zugreifen (hat einen echten NullReferenceException-Bug verursacht).
- **Kein Drag & Drop:** `_CanDropData`/`_DropData` funktionierte im echten Editor nicht (vermutlich falsche Bubbling-Annahme). Verschieben/Interaktion läuft seitdem über explizite Buttons + Events (`ActionClicked`, `Clicked`).
- **Kein Godot-Physik-Kollisionssystem:** bewusst vermieden (ungetestete Layer/Masken) – Nähe/Reichweite immer per `Vector2.DistanceTo()`-Handrechnung (`Player`/`Enemy`/`Projectile`).
- **Keine globale Pause** (`GetTree().Paused`): stattdessen manuelle `SetDisabled(bool)`-Flags auf `Player`/`Enemy`/`Projectile` + `Timer.Stop()`, gesteuert über `Arena.SetWorldPaused()`.
- **Kein Input-Map:** WASD wird direkt per `Input.IsPhysicalKeyPressed(Key.W/A/S/D)` abgefragt statt über `[input]`-Actions in `project.godot`.
- **Gruppen statt Referenzlisten:** `AddToGroup("player"/"enemies")` + `GetTree().GetNodesInGroup(...)` für Player↔Enemy-Discovery.
- **Async/Await für Bestätigungsdialoge:** `await ToSignal(button, Button.SignalName.Pressed)` pausiert eine Methode bis zum Klick – genutzt sowohl im alten Popup als auch im Level-up-"Weiter"-Flow.
- **Tween-API:** `CreateTween().SetParallel(true).TweenProperty(node, "position:y", ziel, dauer)` für einfache Animationen (`FloatingText`), Sub-Properties per String-Pfad in snake_case (`"position:y"`, `"modulate:a"`).
- **Theme:** eine globale Theme-Resource (`theme/game_theme.tres`) über `project.godot` `[gui] theme/custom`, mit benannten `theme_type_variation`-Varianten (z. B. `CardPanel`, `DeskLabel`, `CardActionButton`) statt Styling pro Node.
- **Layout ohne natives "space-between":** `HBoxContainer` kennt das nicht – wird über leere `Control`-Spacer-Nodes mit `SizeFlags.Fill|Expand` zwischen fixen Spalten nachgebaut (siehe `Arena.OnAbilityGained`).
- **Reentrancy bei Kaskaden-Events:** `Player.UpdateAbilities` iteriert über `_abilities`; ein Treffer kann darin aber einen Gegner töten → `GrantXp` → `LevelUp` → `EquipAbility`, was `_abilities` mitten in der Schleife verändert (`InvalidOperationException: Collection was modified`, echter Bug, von Nutzer in Godot reproduziert). Fix: über `_abilities.ToList()` (Kopie) iterieren. Genereller Merksatz: bei jeder Schleife, die Methoden mit Seiteneffekten aufruft, die kaskadierend dieselbe Collection verändern könnten, vorsichtshalber über eine Kopie iterieren.

## Browser-Prototyp (bereits erstellt)

Ein spielbarer HTML/JS-Prototyp existiert bereits (Datei: `dice-and-cards-prototype.html`), der das Karten+Würfel-Kampfsystem testet:
- Ein Charakter (Krieger, D&D-Stats: STR 16, DEX 12, CON 14, INT 10, WIS 10, CHA 8), AC 15, HP 24.
- Gegner: Höhlengoblin (AC 13, HP 12, Angriffsbonus +4, 1W6+2 Schaden).
- Deck aus 10 Karten (Hieb x4, Wuchtschlag x2, Parade x2, Finte x1, Atem holen x1 [Exhaust]).
- Reines Vanilla HTML/CSS/JS, keine Frameworks – Logik lässt sich konzeptionell auf GDScript/C# übertragen.

## Godot-Projekt (bereits aufgesetzt)

Das Godot-4-Projekt liegt im Repo-Root (`project.godot`, `PPRogueLite.csproj`). Ursprünglich wurde hier die 1:1 aus dem Browser-Prototyp übertragene **rundenbasierte** Kampflogik umgesetzt und vom Nutzer erfolgreich in Godot 4.7 getestet (`scenes/Main.tscn`/`Main.cs`, `CombatEngine`, Karten-Popup-System mit "Weiter"-Button) – dieser Teil ist durch den Pivot zu Echtzeit inzwischen **abgelöst** (siehe "Altlasten" unten), war aber die Basis für zwei bis heute genutzte Dinge:
- **Theme** `theme/game_theme.tres`: Papier-&-Schreibtisch-Farbpalette (dunkler Hintergrund, Papier-Panels, Messing-Buttons, grün/rote HP-Balken), vom Nutzer getestet und für gut befunden. Wird von **allen** aktuellen Szenen (Hub, Arena, DeckScreen, Camp) weiterverwendet. Bewusst ohne die Google-Fonts (Special Elite/Crimson Text) aus dem Prototyp – Systemfont, Fonts können später ergänzt werden.
- **Popup-Muster** (Ergebnis zentriert zeigen, erst nach "Weiter"-Klick weiter, kein Timer): Das Prinzip aus dem alten Kampf-Popup lebt im Level-up-Screen der Echtzeit-Arena weiter (siehe dort).

## Hub / Menüstruktur (bereits aufgesetzt)

Das Spiel startet jetzt in `scenes/Hub.tscn` (Startszene laut `project.godot`) statt direkt im Kampf – der Hub ist die "Taverne" aus der Meta-Ebene-Planung und **nur zwischen zwei Dungeons erreichbar** (Zwischenstopps *innerhalb* eines laufenden Dungeons laufen über das Lager, siehe Dungeon-Struktur-Abschnitt unten). Vier Buttons in einem zentrierten Papier-Menüpanel plus Gold-Anzeige darüber (`GoldLabel`, liest `PlayerWallet.Gold`):

- **"Dungeon betreten"** – **funktional**: ruft `DungeonRun.Start()` auf (setzt Stage 1, verwirft alten Fortschritt) und wechselt per `GetTree().ChangeSceneToFile()` zu `scenes/Arena.tscn`, startet damit einen neuen Dungeon in der Echtzeit-Arena (siehe eigener Abschnitt unten). Zeigte früher auf `scenes/Main.tscn` (alter rundenbasierter Kampf) – siehe Pivot oben.
- **"Karten managen"** – **funktional**: wechselt zu `scenes/DeckScreen.tscn` (siehe eigener Abschnitt unten). Nur hier im Hub möglich, nicht im Lager (siehe Dungeon-Struktur-Abschnitt) – Deck-Bearbeitung ist bewusst nur zwischen Dungeons erlaubt.
- **"Gruppe managen"** – *noch ohne Funktion (UI-Platzhalter)*. Geplant: alle besessenen Charaktere als Charakterkarten; Anklicken einer Karte öffnet das Charakterbogen-Sheet (Bezug zur Roster-/Gruppen-Planung in der Meta-Ebene oben).
- **"Mit Loot entkommen"** – *noch ohne Funktion (UI-Platzhalter)*. Geplant: Weg, einen Run kontrolliert/sicher zu beenden (Loot behalten statt Risiko eines Wipes).

Zurück zum Hub geht's über `Arena.ContinueButton` (bei Niederlage oder nach der letzten Stage eines Dungeons) oder über das Lager (`Camp.EndDungeonButton`, Dungeon vorzeitig beenden). Jeder erneute Einstieg über "Dungeon betreten" baut den Run komplett neu auf (frischer Charakter/Deck-Ziehung/Gegner-Spawns).

Vom Nutzer in Godot getestet, funktioniert (Stand vor Issue #3 – die neue Gold-Anzeige und `DungeonRun.Start()`-Anbindung sind neu und noch nicht gegengeprüft).

## Echtzeit-Arena (bereits aufgesetzt, löst den alten Main.tscn-Kampf ab)

Neue Szenen `scenes/Arena.tscn` + `scenes/Player.tscn` + `scenes/Enemy.tscn` + `scenes/FloatingText.tscn` (jeweils mit zugehörigem `.cs`), plus `scenes/DeckStackView.cs`/`scenes/Projectile.cs` (reiner Code, keine eigene `.tscn` nötig):

- **Player** (`Player.cs`, `Node2D`, kein Physik-Kollisionssystem – Bewegung/Reichweite laufen über einfache Distanzberechnung, um ungetestete Kollisions-Layer/Masken zu vermeiden): WASD-Bewegung (`Input.IsPhysicalKeyPressed`, direkt abgefragte Tasten statt Input-Map-Actions), begrenzt auf den Viewport. Visuell ein per `_Draw()` gezeichneter Kreis (Messingfarbe) plus ein halbtransparenter Ring in Angriffsreichweite um den Charakter – keine Sprite-Assets vorhanden.
- **Fähigkeiten statt Handkarten:** Player hält eine Liste aktiver Fähigkeiten (`ActiveAbility`: `CardDefinition` + eigener Cooldown-Timer), die jede für sich automatisch auslöst, sobald ihr Cooldown abläuft (`Player._Process` → `UpdateAbilities`). Start jedes Runs: fest mit "Hieb" ausgerüstet (`new HiebCard()`, kein Zug aus dem Deck nötig – Sicherheitsnetz gegen einen Run ohne Angriff).
- **Fähigkeiten-Leiste im HUD:** oben links zeigt `Arena` (`AbilityBar`, `HBoxContainer`) für jeden **unterschiedlichen** aktiven Kartentyp ein Badge (Name + Stückzahl, falls mehrfach gezogen – z. B. "Hieb ×2" – plus Cooldown-Balken, der sich füllt und beim Auslösen der Fähigkeit zurückspringt – `Player.GetCooldownProgress(cardId)`, pro Frame ausgelesen). Neue Badges kommen von links nach rechts hinzu, sobald ein bisher unbekannter Kartentyp gezogen wird (`Player.NewAbilityTypeUnlocked`, unterscheidet sich von `AbilityGained`, das bei jeder Ziehung inkl. Duplikaten feuert und den Zähler aktualisiert). Bei mehreren Instanzen desselben Kartentyps zeigt der Cooldown-Balken nur die erste/primäre Instanz.
- **Badges sind anklickbar** (`Arena.AddAbilityBadge` hängt sich an das C#-Event `PanelContainer.GuiInput`, gleiches Signal-Prinzip wie `CardView._GuiInput`, hier aber ohne eigene Node-Unterklasse): ein Klick ruft `Player.ToggleAbility(cardId)` auf und schaltet **alle Instanzen** dieses Kartentyps ein/aus (`ActiveAbility.Enabled`, von `UpdateAbilities` übersprungen – der Cooldown-Timer pausiert dabei, statt weiterzulaufen). Deaktivierte Badges werden halbtransparent (`DisabledAbilityModulate`) und zeigen zusätzlich "(aus)" im Label. Gedacht, um einzelne Effekte isoliert zu testen oder alle Angriffe abzuschalten, um kontrolliert Schaden zu nehmen – bewusst **keine reine Testfunktion**, soll auch im fertigen Spiel bleiben, damit Spieler Builds ausprobieren können.
- **XP-Balken im HUD** (`XpLabel`/`XpBar`, unter der HP-Anzeige): zeigt Fortschritt zum nächsten Level (`Player.Xp`/`XpToNextLevel`).
- **Deck-Anbindung:** Beim `_Ready()` baut sich `Player` ein **laufeigenes** `Deck` aus `PlayerCardCollection.DeckCards` (`new Deck(PlayerCardCollection.DeckCards)` – kopiert nur die Zusammensetzung, verändert die Sammlung im Deck-Screen nicht). Bei jedem Level-up (`GrantXp`/`LevelUp`) werden 2 Karten gezogen und der Spieler wählt eine davon aus – siehe eigener Kartenauswahl-Abschnitt unten (Issue #15).
- **Verhalten pro Karte** ist in `Player.TriggerAbility` fest verdrahtet (switch über `CardDefinition.Id`, kein neuer generischer "Echtzeit-Play"-Mechanismus auf `CardDefinition` selbst, um die reine Datenklasse nicht an Godot-Typen zu koppeln): Hieb/Wuchtschlag = Nahkampfangriff auf den nächsten Gegner in Reichweite (verdeckter W20 + STR-Mod gegen RK, Schadenswürfel, Vorteil möglich, Kritischer Treffer bei natürlicher 20 = doppelter Schaden), Parade = temporärer RK-Bonus-Puls, Finte = setzt "Vorteil beim nächsten Treffer", Atem holen = Selbstheilung – jeweils mit eigenem Cooldown.
- **XP/Level:** 1 XP pro getötetem Gegner (`Enemy.TakeDamage` → `Player.GrantXp`, unabhängig vom Gegnertyp), 2 XP pro Level (Test-Balance-Wert, damit sich beim Testen mit ~2 Kills pro Level-up schnell was tut – ursprünglich 5, bewusst gesenkt). Bei einem Level-up **pausiert die Runde**: `Arena.SetWorldPaused(true)` deaktiviert Player (`SetDisabled`), alle Gegner UND alle Projektile (`SetDisabled` über die `"enemies"`-/`"projectiles"`-Gruppen) und den Wellenwechsel-Timer. Der Level-up-Screen nutzt dabei die **volle Bildschirmbreite** (an einer Nutzer-Skizze orientiert, noch kein fertiges Design) – `LevelUpLayer` ist ein `MarginContainer` (nicht mehr `CenterContainer`), die drei Spalten werden über Fill/Expand-`Control`-Spacer zwischen ihnen auf die volle Breite verteilt (Godots `HBoxContainer` kennt kein natives "space-between"):
  - **Links:** drei Kartenstapel-Übersichten ("Nachziehstapel" / "Bereits gezogen" / "Ablage", die dritte seit Issue #15 – siehe eigener Kartenauswahl-Abschnitt unten), jeweils mit einer schrumpfenden Stapel-Grafik (`scenes/DeckStackView.cs`, eigener `Control` mit `_Draw()` – zeichnet eine Karten-Rückseite pro noch vorhandener Karte, leicht versetzt, wird also sichtbar kleiner) plus allen 8 bekannten Kartentypen mit Stückzahl (`CardCatalog.AllCardTypes()`), bei 0 ausgegraut. Zeigt bewusst nur Summen pro Typ, nicht die tatsächliche (gemischte) Reihenfolge der Stapel. "Bereits gezogen" entspricht den aktuell ausgerüsteten Fähigkeiten (`Player.CountEquipped`). Gemeinsam gebaut über `Arena.BuildDeckOverviewColumn()`, wiederverwendet von beiden Level-up-Varianten (Einzelkarte automatisch ausgerüstet vs. echte Auswahl, siehe unten).
  - **Mitte:** entweder die eine automatisch ausgerüstete Karte (`CardView`) + "Weiter"-Button (wenn der Nachziehstapel nur noch eine Karte hergibt, keine echte Wahl möglich), oder – im Regelfall – 2 zur Auswahl stehende Karten nebeneinander, ein Klick entscheidet direkt (siehe Kartenauswahl-Abschnitt unten, Issue #15).
  - **Rechts:** Charakterbogen (Bild-Platzhalter, Name, Klasse, Rüstungsklasse, sechs Attribute) im Format "Wert (Basiswert Delta)" – Delta aktuell fast immer +0 (nur die Rüstungsklasse kann sich durch Parade kurzzeitig erhöhen), Format aber bereits vorbereitet für künftige Boni (grün) / Mali (rot) durch Ereignisse/Ausrüstung.
- **Gegner** (`Enemy.cs`, Issue #9 – siehe eigener Gegner-Abschnitt unten für Details): ein gemeinsamer Node für 5 Gegnertypen, datengetrieben über `EnemyDefinition` (`PPRogueLite.Enemies`). Kein Pathfinding, kein Event für Wellen-Ende (Arena pollt `GetNodesInGroup("enemies")`), zeigt einen kleinen Lebensbalken über sich (per `_Draw()`, aktualisiert bei jedem Treffer über `QueueRedraw()`).
- **Wellen/Stage-Struktur** (`Arena.BeginWave`/`CheckWaveCleared`, Umsetzung von GitHub-Issue #2): eine Stage besteht aus mehreren Wellen, Welle N spawnt N Gegner auf einmal (Welle 1 = 1, Welle 2 = 2, ...). Die Gesamtzahl der Wellen (`Arena._totalWaves`) kommt seit Issue #10 vom gewählten Pfad-Knoten (`DungeonRun.CurrentNode.WaveCount`, 7/10/13 je nach Route, siehe Dungeon-Struktur-Abschnitt unten) statt einer festen Konstante. Die nächste Welle startet erst, wenn die aktuelle vollständig besiegt ist (`CheckWaveCleared` pollt pro Frame `GetTree().GetNodesInGroup("enemies").Count == 0`, kein Event von `Enemy` nötig), dann kurze Pause über `WaveTransitionTimer` (`Arena.tscn`, one-shot, 1,5 s) bevor die nächste Welle spawnt. Welche Gegnertypen dabei gespawnt werden, ist unabhängig von der Wellenanzahl (siehe Gegner-Abschnitt unten). Aktuelle Welle wird im HUD angezeigt (`WaveLabel`, "Welle: N / M"). Nach der letzten Welle endet die Stage als **Sieg** (siehe HUD unten) – vorher lief die Arena endlos ohne Sieg-Bedingung, das ist jetzt abgelöst.
  - **⚠️ Aktuell aktiver Test-Override:** `Arena.TestWaveCountOverride = 2` überschreibt die tatsächlich gespielte Wellenanzahl jeder Stage auf 2, unabhängig vom Pfad-Knoten (nur zum schnelleren manuellen Testen). Lager/Routenwahl zeigen weiterhin die echte Wellenanzahl des Knotens an (7/10/13) – nur die Arena spielt kürzer. Rückgängig machen: `TestWaveCountOverride` in `Arena.cs` auf `0` setzen oder die Zeile in `_Ready()` löschen, die den Override anwendet. **Vor einem "richtigen" Balancing-Durchgang unbedingt zurücksetzen.**
- **Feedback statt Popup:** `FloatingText.cs` (kurzer, aufsteigender/verblassender Text) zeigt Schaden/"Verfehlt"/Heilung/Buff-Namen direkt am Ort des Geschehens – ersetzt das alte Log-/Popup-System vollständig.
- **HUD** (`Arena.tscn`, `CanvasLayer`): Fähigkeiten-Leiste, HP-Balken/-Text, XP-Balken/-Text, Stage-Anzeige, Wellen-Anzeige, Überlebenszeit, Ergebnis-Text (`OutcomeLabel`, "Niederlage"/"Stage N von 10 abgeschlossen! +N Gold"/"Dungeon abgeschlossen! +N Gold") und ein Button mit dynamischem Text/Ziel (`ContinueButton`, gesteuert über `Arena.FinishRun(text, buttonText, targetScene)` – "Weiter zum Lager" → `Camp.tscn` bei einer Nicht-Schluss-Stage, sonst "Zurück zur Taverne" → `Hub.tscn`).
- **Gold-Belohnung:** `PPRogueLite.Meta.PlayerWallet` (static class, gleiches Muster wie `PlayerCardCollection`) hält `Gold` prozessweit. Bei Stage-Abschluss gibt's `Arena.BaseGoldReward = 25` (Test-Balance-Wert) multipliziert mit dem Gold-Multiplikator des gewählten Pfad-Knotens (`DungeonRun.CurrentNode.GoldMultiplier`, Issue #10) – noch nicht ausgegeben (folgt mit Card Shop/Issue #4). Wird im Hub und im Lager angezeigt.

**Bewusste Vereinfachungen dieser ersten Version** (nicht vergessen, wenn's ans Polishing geht): keine Godot-Physik/Kollisionslayer (nur Distanzchecks, Gegner können sich gegenseitig überlappen), kein Sprite/Animationen (nur gezeichnete Kreise/Formen), Cooldown-Balken bei doppelten Kartentypen zeigt nur die erste Instanz. Ausgerüstete Fähigkeiten starten bei jeder neuen Stage mit vollem Cooldown neu (kein Versuch, den exakten Timer-Stand über den Szenenwechsel zu retten).

Bewegung/Angriffe/Ability-Leiste/Cooldowns/XP/Level-up-Pause/Deck-Stapel-Grafik/Game-Over-Flow vom Nutzer in Godot getestet, funktioniert (iterativ über mehrere Runden verfeinert, siehe Balance-Werte oben: Goblin-RK 8, 2 XP/Level). Das Wellen-/Stage-System (Issue #2), die Mehrfach-Stage-Anbindung (Issue #3), die Pfad-Verzweigung mit variabler Wellenanzahl/Gold (Issue #10, siehe Dungeon-Struktur-Abschnitt unten) und die Gegner-Vielfalt (Issue #9, siehe eigener Abschnitt unten) sind neu und **noch nicht in der echten Godot-Umgebung gegengeprüft**.

## Gegner-Vielfalt (bereits aufgesetzt, Issue #9)

Fünf Gegnertypen (`PPRogueLite.Enemies`, kein Godot-Bezug), alle über den gemeinsamen `Enemy.cs`-Node gespielt (siehe "Gleiches Muster bei Gegnern" oben). Jeder Typ ist eine `EnemyDefinition`-Unterklasse mit Stats + einem `EnemyMovement`-Bewegungsmuster + optionalem `EnemyOnHitEffect`:

- **Höhlengoblin** (`goblin`, Melee, ab Stage 1) – der ursprüngliche Gegner aus Issue #2, unverändert (RK 8, 12 HP, Angriffsbonus +4, 1W6+2). Läuft direkt auf den Spieler zu, Kontaktangriff.
- **Höhlentroll** (`troll`, Melee, ab Stage 3) – Tank: viel HP (30), höhere RK (10), aber langsam (halbe Goblin-Geschwindigkeit) und geringerer Schaden pro Treffer (1W8+1). Rein stat-basiert, keine neue Mechanik.
- **Goblin-Bogenschütze** (`archer`, Ranged, ab Stage 2) – hält Abstand (`EngagementRange` 260px ± Toleranz: nähert sich/weicht zurück je nach Distanz) und feuert Pfeile (`Projectile`, siehe unten) statt Kontaktangriff. Wenig HP (8), fragil.
- **Kobold-Schamane** (`schamane`, Ranged + `EnemyOnHitEffect.Slow`, ab Stage 4) – wie der Bogenschütze, aber größerer Wunschabstand (300px), langsamerer Cooldown, dafür verlangsamt ein Treffer den Spieler kurzzeitig (`Player.ApplySlow(duration, multiplier)`, 2 s auf 50 % Geschwindigkeit – gleiches Timer-Muster wie `_paradeTimer`).
- **Kobold-Schurke** (`schurke`, HitAndRun, ab Stage 3) – schnell (110px/s), nähert sich, greift bei Kontakt an, zieht sich danach für 1 s aktiv zurück statt stehen zu bleiben (`_retreatTimer` in `Enemy.cs`). Macht bewusst nur normalen Schaden – kein Gold-Diebstahl in dieser ersten Version (auf Nutzerwunsch als mögliche spätere Ergänzung zurückgestellt, siehe Offene Punkte unten).

**Gestaffelte Einführung:** `EnemyDefinition.MinStage` legt fest, ab welcher Dungeon-Stage ein Typ im Spawn-Pool auftaucht; `EnemyCatalog.AvailableForStage(DungeonRun.CurrentStage)` filtert den Pool, `Arena.SpawnEnemy()` würfelt pro Gegner zufällig aus diesem Pool. Stage 1 spawnt dadurch garantiert nur Goblins (einziger Typ mit `MinStage == 1`), ab Stage 2 kommen nach und nach weitere Typen dazu und werden gemischt gespawnt (auf Nutzerwunsch so festgelegt).

**Projektile** (`scenes/Projectile.cs`, reiner Code wie `DeckStackView`): fliegen geradlinig in eine feste Richtung (kein Homing, keine Godot-Physik), Treffer wird per Distanz zum Spieler geprüft (`HitRadius`), lösen denselben verdeckten W20-Wurf wie ein Kontaktangriff aus. Werden über die `"projectiles"`-Gruppe von `Arena.SetWorldPaused` mitpausiert, damit sie beim Level-up-Screen nicht weiterfliegen.

**Bewusste Vereinfachungen:** keine Sprites (weiterhin gezeichnete Kreise, `Radius` steht auf `EnemyDefinition`, die Farbe kommt aus einer kleinen `ColorFor(id)`-Lookup-Tabelle in `Enemy.cs` statt aus `EnemyDefinition` selbst – `Godot.Color` würde die sonst Godot-freie Datenklasse an Godot koppeln), Projektile sind nicht homing, alle Gegnertypen geben gleich viel XP (1 pro Kill, kein Wert-Unterschied nach Schwierigkeit).

## Boss-Kampf (bereits aufgesetzt, Issue #11)

Planungsgespräch: drei Boss-Konzepte vorgeschlagen (Oger-Häuptling/Junger Drache/Kobold-Hexenmeister), Nutzer wollte **alle drei** – Umsetzung startet mit dem **Oger-Häuptling**, Drache und Hexenmeister sind für spätere Bosse **vorgemerkt** (eigene Spezialmechaniken – Feueratem-Telegraph bzw. Teleport-Ausweichen – noch nicht gebaut). Nutzer-Vorgabe zur Stage-Struktur: **keine normalen Wellen mehr vor dem Boss** – die letzte Stage besteht nur noch aus dem Bosskampf.

- **Architektur:** `BossDefinition` (`PPRogueLite.Enemies`, abstract, erbt von `EnemyDefinition`) erweitert die normalen Kampfwerte um boss-spezifische Felder (Summon-/Slam-Cooldowns, Slam-Geometrie/-Schadenswerte) und legt `Movement` fest auf einen neuen `EnemyMovement.Boss`-Fall. Der gemeinsame `Enemy.cs`-Node (Issue #9) bekommt dafür `UpdateBoss` als weiteren Fall im Bewegungs-Switch – kein separater Boss-Node nötig, gleiches Muster wie bei den 5 regulären Gegnertypen. `OgerHaeuptlingDefinition` ist die erste (und bisher einzige) `BossDefinition`-Unterklasse (140 HP, RK 13, Angriffsbonus +6, 1W8+4, Radius 34, Geschwindigkeit 55px/s – Test-Balance-Werte, deutlich tankier als der Höhlentroll).
- **Normaler Kontaktangriff:** wie jeder Melee-Gegner (`UpdateMelee`/`AttackPlayer`, unverändert wiederverwendet).
- **Verstärkung rufen:** alle `SummonCooldown` (12 s) ruft der Oger `SummonCount` (2) Exemplare von `SummonedEnemy` (Höhlengoblin) mit leichtem Zufalls-Versatz um seine Position herum – `Arena.SpawnEnemyNear(definition, position, scatterRadius)` (neu, public, generisch für beliebige `EnemyDefinition` statt hart auf Goblin verdrahtet) instanziert `Enemy.tscn` genauso wie der normale Wellen-Spawn, nur ohne Rand-Spawn-Position. Der Boss braucht dafür eine Referenz auf `Arena` – `Enemy._Ready()` setzt `_arena = GetParent() as Arena` (Arena ist immer der direkte Parent, da `Arena.SpawnEnemy()`/`SpawnBoss()` per `AddChild(enemy)` direkt unter sich einhängen).
- **Keulenschlag (telegraphierter Nahangriff):** alle `SlamCooldown` (7 s) legt der Boss `SlamDirection` einmalig Richtung Spieler fest und zeigt für `SlamTelegraphDuration` (1 s) eine rot-transparente, rechteckige Hitbox nach vorne (`SlamRange` 220px lang, `SlamHalfWidth` 55px breit) über `Enemy._Draw()` (`DrawColoredPolygon`, vier per Rotationsformel berechnete Eckpunkte). Der Boss bewegt sich während Wind-up/Auflösung nicht (`UpdateBoss` überspringt `UpdateMelee` in diesem Zustand), damit die angezeigte Hitbox verlässlich stimmt. Nach Ablauf der Telegraph-Zeit prüft `ExecuteSlam` nur noch, ob der Spieler **zu diesem Zeitpunkt** geometrisch in der Box steht (`IsInSlamHitbox`, Skalarprodukt-basierter Rechteck-Test) – wer rechtzeitig wegläuft, wird nicht getroffen, ganz ohne Würfelwurf. Wer getroffen wird, bekommt trotzdem den üblichen verdeckten W20 + `SlamAttackBonus` gegen RK (schwerer Schaden: 1W12+8) – das Ausweichen läuft also rein über Positionierung, der Würfel entscheidet nur noch, ob der (schon getroffene) Schlag zusätzlich danebengeht.
- **Stage-10-Struktur:** `Arena._isBossStage = DungeonRun.CurrentStage == DungeonRun.TotalStages`; in diesem Fall wird `_totalWaves` fest auf **1** gesetzt (unabhängig von `TestWaveCountOverride`) und `BeginWave(1)` ruft statt der üblichen Zufalls-Spawns `SpawnBoss()` (instanziert `OgerHaeuptlingDefinition` direkt, kein Catalog-Eintrag nötig, da noch kein zweiter Boss existiert). Der bestehende `CheckWaveCleared`/`CompleteStage`-Ablauf greift dadurch unverändert: sobald die `"enemies"`-Gruppe leer ist (Boss + evtl. noch lebende Verstärkung besiegt) und `_currentWave (1) >= _totalWaves (1)`, gilt die Stage als abgeschlossen. `WaveLabel` zeigt auf der Boss-Stage "Boss-Kampf" statt "Welle: N / M".
- **HP-Balken skaliert mit:** `Enemy._Draw()` berechnet die Balkenbreite jetzt aus `Math.Max(30, Radius × 1,4)` statt fester 30px, damit der deutlich größere Oger-Kreis (Radius 34 vs. 12–22 bei den regulären Typen) einen sichtbar breiteren Balken bekommt.
- **Pause/Level-up:** braucht keine Sonderbehandlung – der Boss steckt wie jeder Gegner in der `"enemies"`-Gruppe, `Arena.SetWorldPaused` deaktiviert ihn also automatisch mit; Summon-/Slam-Timer frieren während der Pause korrekt ein, da `Enemy._Process` bei `_disabled` komplett überspringt.

Noch nicht in der echten Godot-Umgebung getestet.

## Dungeon-Struktur (bereits aufgesetzt, Issue #3 + #10)

Ein Dungeon besteht aus `DungeonRun.TotalStages` = **10** Stages hintereinander (jede Stage = ein voller Arena-Durchlauf, Wellenanzahl variiert je nach gewählter Route – siehe Pfad-Struktur unten). Neuer Zustand `PPRogueLite.Meta.DungeonRun` (static class, gleiches Muster wie `PlayerCardCollection`/`PlayerWallet`) hält den Fortschritt über die Szenenwechsel Arena ↔ Lager hinweg fest, da eine neue Stage eine neue `Arena`-Szene (und damit eine neue `Player`-Instanz) bedeutet.

**Pfad-Struktur (Issue #10):** Statt einer festen linearen Kette generiert `DungeonRun.Start()` einmalig einen verzweigten Knoten-Graph über `DungeonMapGenerator.Generate()` (`PPRogueLite.Dungeon`, kein Godot-Bezug):
- Spalte 1 (Start) und Spalte 10 (Boss-Stage, siehe Boss-Kampf-Abschnitt oben) sind je ein einzelner Knoten **ohne Modifikatoren** (Standard-Wellenanzahl, Gold ×1,0 – "erste Stage ist immer ohne Modifikatoren"; die Boss-Stage überschreibt die Wellenanzahl ohnehin auf 1 Encounter, siehe oben).
- Spalten 2–9 haben je 3–4 Knoten (`StageNode`), jeder mit einem zufälligen Risk/Reward-Preset: **Kurz** (7 Wellen, Gold ×1,0), **Standard** (10 Wellen, Gold ×1,3), **Lang** (13 Wellen, Gold ×1,6) – mehr Wellen lohnt sich also mit mehr Gold. Bewusst auf bereits existierende Werte (Wellenanzahl, Gold) beschränkt, da Gegner-Vielfalt (#9) und Kartenshop (#4) noch nicht existieren.
- Jeder Knoten bekommt 1–2 zufällige Verbindungen zur nächsten Spalte (`StageNode.NextNodeIds`); anschließend wird sichergestellt, dass jeder Knoten mindestens eine eingehende Verbindung hat, damit garantiert jeder Knoten von Spalte 1 aus erreichbar ist (Details/Beweisskizze im Doc-Kommentar von `DungeonMapGenerator`).
- `DungeonRun.CurrentNodeId` zeigt auf den Knoten der aktuell laufenden/gerade abgeschlossenen Stage; `DungeonRun.CurrentNode.WaveCount`/`.GoldMultiplier` werden von `Arena._Ready()`/`Arena.CompleteStage()` gelesen, um Wellenanzahl bzw. Gold-Belohnung dieser Stage zu bestimmen.
- Es wird **nicht** die volle Graph-Ansicht wie in der Skizze aus dem Issue gerendert (kein Node-Diagramm) – bewusste Vereinfachung, um nicht zusätzlich eine grafische Graph-UI bauen zu müssen. Stattdessen zeigt das Lager nur die konkret erreichbaren nächsten Routen als Buttons (siehe unten).

**Ablauf:**
- **Start eines Dungeons:** `Hub.OnEnterDungeonPressed` ruft `DungeonRun.Start()` (generiert den Pfad-Graph, setzt `CurrentStage = 1`, `CurrentNodeId` auf den Spalte-1-Knoten, `HasProgress = false`) und wechselt zu `Arena.tscn`. `Player._Ready()` sieht `HasProgress == false` und baut wie bisher einen frischen Charakter (volles HP, Level 1, nur Hieb ausgerüstet, frisches Deck aus `PlayerCardCollection.DeckCards`).
- **Stage-Abschluss, Dungeon geht weiter** (`Arena.CompleteStage`, `DungeonRun.CurrentStage < TotalStages`): Gold gutschreiben (Basisbetrag × `CurrentNode.GoldMultiplier`), `Player.SaveProgress()` schreibt HP/XP/Level/Nachziehstapel/ausgerüstete Fähigkeiten in `DungeonRun` (setzt `HasProgress = true`), dann `Arena.FinishRun(...)` mit Button "Weiter zum Lager" → `Camp.tscn`. Die Stage-Nummer wird hier **noch nicht** erhöht – das passiert erst bei der Routenwahl im Lager.
- **Lager, Routenwahl** (`Camp.tscn`/`Camp.cs`, "Lager"): zeigt "Stage N von 10 abgeschlossen", den aktuellen Gold-Stand, und – als anklickbare Buttons – `DungeonRun.GetNextChoices()` (die vom aktuellen Knoten aus erreichbaren nächsten Routen, meist 1–2, jeweils mit "Route A/B: N Wellen · Gold ×M" beschriftet). Klick ruft `DungeonRun.SelectNextNode(id)` auf (setzt `CurrentNodeId`, erhöht `CurrentStage`) und wechselt zu `Arena.tscn` (neue `Player`-Instanz liest `DungeonRun.HasProgress == true` und stellt HP/XP/Level/Deck/Fähigkeiten wieder her – `Deck`-Konstruktor mit `shuffleOnCreate: false`, damit der gespeicherte Nachziehstapel nicht neu gemischt wird). Separater Button **"Dungeon beenden"** → `DungeonRun.End()` + zurück zu `Hub.tscn` (bereits verdientes Gold bleibt erhalten, da es schon pro Stage direkt in `PlayerWallet` landet).
- **Letzte Stage abgeschlossen** (`CurrentStage == TotalStages`): `DungeonRun.End()`, `Arena.FinishRun(...)` mit Text "Dungeon abgeschlossen!" und Button "Zurück zur Taverne" → `Hub.tscn` direkt (kein Umweg über das Lager für die letzte Stage).
- **Niederlage:** beendet den Dungeon immer (`DungeonRun.End()`), unabhängig davon, welche Stage gerade lief – zurück zur Taverne. Kein Fortschritt aus der verlorenen Stage wird gespeichert (kein `SaveProgress()`-Aufruf).
- **Absicherung für direkte Tests:** Wird `Arena.tscn` in Godot direkt gestartet (z. B. F6) statt über den Hub, ist `DungeonRun.CurrentStage` noch 0 – `Arena._Ready()` ruft in diesem Fall selbst `DungeonRun.Start()` auf, damit Stage-Anzeige/Lager-Texte trotzdem stimmen.

**Bewusste Vereinfachung:** Keine Heilung zwischen Stages – Schaden aus einer Stage bleibt bis zum Dungeon-Ende bestehen (höheres Risiko, klassischeres Rogue-lite-Gefühl). Falls sich das beim Testen zu hart anfühlt, ist das leicht zu ändern (z. B. `Character.Heal(...)` beim Lager-Eintritt).

Noch nicht in der echten Godot-Umgebung getestet.

## Altlasten: alter rundenbasierter Kampf (nicht mehr erreichbar, noch nicht gelöscht)

Durch den Pivot zu Echtzeit sind folgende Dateien **von keinem Menü mehr erreichbar**, aber bewusst noch nicht gelöscht (falls doch etwas daraus wiederverwendet werden soll): `scenes/Main.tscn`/`Main.cs` (komplette alte Kampf-UI inkl. Popup-System), `scripts/combat/CombatEngine.cs`, `RollStamp.cs`, `LogTag.cs`, `AttackResult.cs`, `AbilityCheckResult.cs` (alle eng an den Popup-/Log-Ablauf gekoppelt). **Weiterhin genutzt** und NICHT verwaist: `scripts/character/*`, `scripts/combat/Dice.cs`, `scripts/cards/*` (Card-Definitionen + `Deck`-Klasse, jetzt Grundlage der Echtzeit-Fähigkeiten), `scenes/DeckScreen.*`, `scenes/CardView.*`, `scripts/meta/PlayerCardCollection.cs`.

## Deck-Screen (bereits aufgesetzt)

`scenes/DeckScreen.tscn` (über "Karten managen" im Hub erreichbar) zeigt zwei Spalten in Papier-Panels:
- **Links:** alle Karten im aktuellen Kampf-Deck ("Im Deck (N)").
- **Rechts:** alle besessenen Karten, die *nicht* im Deck sind ("Nicht im Deck (N)").
- Gleiche Kartentypen werden gruppiert mit Stückzahl gezeigt (z. B. "Hieb ×2" im Deck, "Hieb ×3" im Bestand), statt jede physische Karteninstanz einzeln aufzulisten.
- **Karten verschieben:** kein Drag & Drop (erste Umsetzung über Godots `_CanDropData`/`_DropData` hat im echten Editor nicht funktioniert – "kann hier nicht hin"-Cursor, vermutlich Ancestor-Bubbling-Annahme falsch). Stattdessen hat jede Karte oben rechts ein kleines Action-Badge: **"×"** im Deck (entfernt eine Karteninstanz aus dem Deck, legt sie in den Bestand), **"+"** im Bestand (nimmt eine Karteninstanz ins Deck auf). Klick verschiebt genau eine Karteninstanz und rendert beide Spalten neu.
- **Deckgrößen-Regel:** Das Deck darf beim Bearbeiten größer oder kleiner werden, die Anzahl über der Deck-Spalte wird bei mehr als `MaxDeckSize` (10) Karten rot. "Zurück zum Hub" ist deaktiviert, solange das Deck außerhalb von `MinDeckSize`–`MaxDeckSize` liegt; ein Hinweistext unter den Spalten erklärt warum. `MinDeckSize` ist bewusst auf **1** gesenkt (statt fest 10), damit sich auch kleinere Testdecks zusammenstellen lassen – z. B. nur die drei neuen Modifikatorkarten (Issue #12), um sie isoliert zu testen.
- "Zurück zum Hub" wechselt zurück zu `scenes/Hub.tscn` (nur klickbar, wenn die Deckgröße zwischen `MinDeckSize` und `MaxDeckSize` liegt).

Datengrundlage ist weiterhin `PPRogueLite.Meta.PlayerCardCollection` (`scripts/meta/PlayerCardCollection.cs`) – eine statische Klasse, deren Felder den Prozess über Szenenwechsel hinweg überleben. Sie hält zwei Listen (`DeckCards`, `BenchCards`), befüllt über `CardCatalog.BuildWarriorStartingDeck()` (10 Karten) und `CardCatalog.BuildWarriorBenchCards()` (10 zusätzliche Karten: 2× Hieb, 2× Wuchtschlag, 1× Parade, 1× Finte, 1× Atem holen, plus die drei Modifikatorkarten aus Issue #12 – Kampfrausch/Explosive Heilung/Adrenalin).

**Wiederverwendbare Kartenansicht `scenes/CardView.tscn`/`CardView.cs`:** zeigt Typ/Name/Beschreibung/Anforderung direkt auf der Karte (keine Hover-Info nötig) und optional eine Stückzahl. Aufbau: äußerer `Control` mit einem `PanelContainer` (Kartentext, `CardPanel`-Theme-Variante) und einem kleinen `Button` oben rechts (`ActionButton`, `CardActionButton`-Theme-Variante, per `ShowAction("×"/"+")` sichtbar geschaltet, Klick löst `ActionClicked` aus). Wird auch in der Echtzeit-Arena wiederverwendet, um gezogene Karten beim Level-up anzuzeigen (dort ohne Action-Badge, reine Anzeige).

**Jetzt verbunden:** Die Echtzeit-Arena (`Player.cs`) liest `PlayerCardCollection.DeckCards` beim Run-Start (kopiert die Zusammensetzung in ein laufeigenes `Deck`) – Karten, die im Deck-Screen verschoben werden, wirken sich also auf den **nächsten** Run aus (siehe Echtzeit-Arena-Abschnitt oben). Der alte rundenbasierte Kampf (`Main.cs`) hatte diese Verbindung nie bekommen und ist inzwischen ohnehin nicht mehr erreichbar (siehe Altlasten oben).

Vom Nutzer in Godot getestet, funktioniert (×/+-Buttons, Stückzahl-Anzeige, 10-Karten-Sperre).

## Modifikatorkarten (bereits aufgesetzt, Issue #12)

Die 4 Kartentypen aus dem Issue (attack/defense/ability/modifier cards) ordnen sich so ein:

- **Angriffskarten** (Hieb, Wuchtschlag) und **Verteidigungskarten** (Parade) waren bereits etabliert – Nahkampfangriff bzw. temporärer RK-Bonus, jeweils eigener Cooldown.
- **Fähigkeitskarten** – "sonstige aktive Effekte, die weder reiner Schaden noch reine Verteidigung sind" (Finte, Atem holen).
- **Modifikatorkarten** – der einzige Typ ohne Entsprechung im bisherigen System, jetzt umgesetzt mit **allen drei** vom Nutzer gewünschten Spielarten:
  1. **Globale passive Boni** (z. B. dauerhaft +1 auf Angriffswürfe).
  2. **An eine bestimmte andere Karte gekoppelt** (z. B. "AOE-Schaden um den Charakter bei Atem holen") – ermöglicht gezielte Build-Synergien.
  3. **Reaktiv auf Ereignisse** (z. B. "+30 % Bewegungsgeschwindigkeit für 2 s nach einem kritischen Treffer").

**Architektur:** `CardDefinition` hat jetzt `Kind` (`CardKind.Action` vs. `CardKind.Modifier`, virtuell mit Default `Action` – bestehende Karten brauchten dadurch keine Änderung) und `CoupledCardId` (nur für gekoppelte Modifikatoren gesetzt, sonst `null`). `Player.EquipCard(card, announce)` verdrahtet eine gezogene Karte je nach `Kind`: Aktionskarten laufen wie bisher über `EquipAbility`/`ActiveAbility` mit eigenem Cooldown; Modifikatorkarten laufen über `EquipModifier` in eine separate `_modifiers`-Liste, **ohne** Cooldown-Slot und **ohne** Eintrag in der Fähigkeiten-Leiste (kein `NewAbilityTypeUnlocked`) – nur `AbilityGained` feuert weiterhin, damit der Level-up-Screen die gezogene Karte trotzdem anzeigt.
- **Globale passive Boni:** `Player.ApplyModifierEffect(card)` wird einmalig beim Ausrüsten aufgerufen und setzt z. B. `_bonusAttackRoll`, der in `TriggerAbility` (Hieb/Wuchtschlag) mit in den Angriffswurf einfließt.
- **Gekoppelte Modifikatoren:** `Player._modifiersByTargetCardId` (`Dictionary<string, List<string>>`) hält "Modifikator Y hängt an Karte X"; `TriggerAbility` fragt das bei der jeweiligen Aktionskarte zusätzlich ab (`ModifiersFor(cardId)` → `ApplyCoupledEffect(modifierId)`).
- **Reaktive Modifikatoren:** kleine Hooks in `Player` (z. B. `OnCriticalHit()` in `ResolveMeleeAttack`, wenn eine natürliche 20 fällt), die per `HasModifier(id)` prüfen, ob der Spieler den passenden Modifikator ausgerüstet hat – gleiches Timer-Muster wie der bestehende Parade-Bonus/Slow-Effekt (`_hasteTimer`/`_hasteMultiplier`, analog zu `_slowTimer`/`_slowMultiplier`).
- `CountEquipped`/`SaveProgress` wurden erweitert, damit Modifikatoren genauso wie Aktionskarten in der "Bereits gezogen"-Übersicht mitgezählt und über Stage-Wechsel hinweg gespeichert/wiederhergestellt werden (`DungeonRun.SavedEquippedCards` enthält jetzt beide Kartenarten gemischt, `Player._Ready()` sortiert beim Wiederherstellen über `EquipCard` automatisch richtig ein).

**Wichtige Abgrenzung:** Gift/Brand/Blutung (Beispiel für gekoppelte Modifikatoren aus der Planung) brauchen zusätzlich ein eigenes Schaden-über-Zeit-System auf `Enemy.cs` – bewusst als **eigenes Issue #14** ausgelagert. "Mit fehlender HP skalierender Angriffsbonus" ist technisch keine eigene (vierte) Spielart, sondern eine Variante der globalen passiven Boni mit einer Formel statt einer festen Zahl – noch nicht als Karte umgesetzt.

**Drei Startkarten** (`scripts/cards/KampfrauschCard.cs`/`ExplosiveHeilungCard.cs`/`AdrenalinCard.cs`), je eine pro Spielart, alle Test-Balance-Werte:
- **Kampfrausch** (global): dauerhaft +1 auf alle Angriffswürfe.
- **Explosive Heilung** (gekoppelt an `atemholen`): Atem holen verursacht zusätzlich 6 Schaden an allen Gegnern im Umkreis von 140px um den Charakter.
- **Adrenalin** (reaktiv auf kritische Treffer): nach einem kritischen Treffer 2 s lang +30 % Bewegungsgeschwindigkeit.

Alle drei starten im Kartenbestand (`CardCatalog.BuildWarriorBenchCards()`), nicht im Startdeck – müssen also erst über den Deck-Screen aktiv ins 10-Karten-Deck geholt werden, bevor sie in einem Run gezogen werden können.

**Optische Abgrenzung:** Modifikatorkarten haben eine eigene `theme_type_variation` (`ModifierCardPanel` in `theme/game_theme.tres`, violett-gräulicher Ton statt des üblichen Papier-Beige) – `CardView.Populate()` wählt die Panel-Variante anhand von `card.Kind`. Kein Formunterschied, wie gewünscht.

Noch nicht in der echten Godot-Umgebung getestet.

## Kartenauswahl beim Level-up (bereits aufgesetzt, Issue #15)

`Player.LevelUp()` zieht jetzt `LevelUpCardChoices` = **2** Karten (`_runDeck.DrawHand(2, allowReshuffleFromDiscard: false)`) statt automatisch eine auszurüsten:

- **Echte Wahl (Regelfall, 2 Karten gezogen):** `Player` feuert `CardChoiceOffered` (neues Event, `IReadOnlyList<CardDefinition>`) statt `AbilityGained`. `Arena.OnCardChoiceOffered` pausiert die Runde wie beim bisherigen Level-up-Screen, zeigt aber beide gezogenen Karten (`CardView`, ohne Action-Badge) **nebeneinander** in der Mittelspalte statt einer Karte + "Weiter"-Button – ein Klick auf eine Karte entscheidet direkt (`CardView.Clicked`, bisher seit dem Echtzeit-Pivot ungenutztes Event, jetzt reaktiviert; kein Polling, sondern `TaskCompletionSource<CardDefinition>` + `await`). Die Wahl geht an `Player.ResolveCardChoice(chosen, candidates)`: die gewählte Karte wird wie bisher über `EquipCard` ausgerüstet, alle anderen wandern über die neue `Deck.Discard(card)` auf die Ablage.
- **Kein echter Choice (Nachziehstapel liefert nur noch 1 Karte):** Fallback auf den bisherigen Einzelkarten-Flow (`AbilityGained` → `Arena.OnAbilityGained`, Karte + "Weiter"-Button) – bei nur einer Option gibt's nichts zu wählen.
- **Ablagestapel ist bewusst dauerhaft, kein Auto-Reshuffle:** `Deck.DrawHand` bekam einen neuen Parameter `allowReshuffleFromDiscard` (Default `true`, damit der alte rundenbasierte Kampf/`Main.cs` unverändert weiterläuft); die Echtzeit-Arena ruft explizit mit `false` auf. Nicht gewählte Karten sind damit für den Rest des Runs unerreichbar, bis eine künftige Karte sie gezielt zurückholt – dafür wurden zwei neue Issues angelegt: **#21** (Karte mischt die komplette Ablage zurück ins Deck) und **#22** (Karte lässt gezielt eine einzelne Karte aus der Ablage zurückholen), beide noch nicht umgesetzt.
- **Ablage übersteht Stage-Wechsel:** `DungeonRun.SavedDiscardPile` (neu, gleiches Muster wie `SavedDrawPile`) wird in `Player.SaveProgress()` geschrieben und beim Aufbau der nächsten Stage in den neuen `Deck`-Konstruktor-Parameter `discardedCards` gegeben – abgelegte Karten bleiben also über den ganzen Dungeon hinweg abgelegt, nicht nur innerhalb einer Stage.
- **Level-up-Screen-Übersicht erweitert:** Die linke Spalte zeigt jetzt drei Stapel-Übersichten statt zwei ("Nachziehstapel" / "Bereits gezogen" / "Ablage", `Player.CountInDiscardPile`) – gemeinsamer Code über `Arena.BuildDeckOverviewColumn()`, von beiden Level-up-Varianten genutzt.

Noch nicht in der echten Godot-Umgebung getestet.

## Offene Punkte / nächste Schritte

**GitHub Issues sind jetzt das Backlog** für größere Features (Repo `Sventie/P-P-Rogue-Lite`, Issues #2–#22). Gemeinsam mit dem Nutzer erarbeitete Abarbeitungsreihenfolge (nach technischen Abhängigkeiten, nicht nach Issue-Nummer). Issues #14–#22 sind neu entstanden aus vorher nur hier im Dokument notierten offenen Punkten/Entscheidungen bzw. als Fast-Follows während der Umsetzung – siehe jeweilige Issue-Beschreibung für Details, hier nur kurz zusammengefasst:

1. ~~**#2 Add Stage logic**~~ – **umgesetzt** (siehe Echtzeit-Arena-Abschnitt oben: Stage aus mehreren Wellen, Welle N = N Gegner, Gold-Belohnung bei Abschluss). Noch nicht vom Nutzer in Godot gegengeprüft.
2. ~~**#3 Add Dungeon logic**~~ – **umgesetzt** (siehe Dungeon-Struktur-Abschnitt oben: mehrere Stages pro Dungeon, neues Lager `Camp.tscn` als Zwischenstopp *innerhalb* eines Dungeons, Taverne/Hub nur noch *zwischen* Dungeons, Charakter-Fortschritt über `DungeonRun` erhalten). Noch nicht vom Nutzer in Godot gegengeprüft.
3. ~~**#10 Add Dungeonpath**~~ – **umgesetzt** (siehe Dungeon-Struktur-Abschnitt oben: verzweigter Knoten-Graph über 10 Stages statt linearer Kette, `DungeonMapGenerator`, Routenwahl im Lager mit Wellenanzahl-/Gold-Modifikatoren; `TotalStages` dafür von 5 auf 10 erhöht). Noch nicht vom Nutzer in Godot gegengeprüft. Bewusst **ohne** grafische Graph-/Node-Map-Ansicht wie in der Issue-Skizze – nur die konkret erreichbaren nächsten Routen werden als Buttons gezeigt.
4. ~~**#9 Add different enemies**~~ – **umgesetzt** (siehe Gegner-Vielfalt-Abschnitt oben: 5 Typen – Goblin/Troll/Bogenschütze/Schamane/Schurke – über einen gemeinsamen `Enemy.cs`-Node, gestaffelt nach Dungeon-Stage freigeschaltet und ab Freischaltung gemischt gespawnt). Noch nicht vom Nutzer in Godot gegengeprüft.
5. ~~**#11 Add boss fight in stage 10**~~ – **umgesetzt** (siehe Boss-Kampf-Abschnitt oben: Oger-Häuptling als erster von drei geplanten Bossen, `BossDefinition`/`EnemyMovement.Boss`, Verstärkung rufen + telegraphierter Keulenschlag, Stage 10 besteht nur noch aus dem Bosskampf ohne vorherige normale Wellen). **Junger Drache** und **Kobold-Hexenmeister** sind als weitere Bosse vorgemerkt (eigene Spezialmechaniken – Feueratem-Telegraph bzw. Teleport-Ausweichen – noch nicht gebaut, keine eigenen Issues bisher). Noch nicht vom Nutzer in Godot gegengeprüft.
6. ~~**#12 add new cards**~~ – **umgesetzt** (siehe Modifikatorkarten-Abschnitt oben: `CardKind` Action/Modifier, alle drei Modifikator-Spielarten mit je einer Startkarte – Kampfrausch/Explosive Heilung/Adrenalin –, optische Abgrenzung über eigene Kartenfarbe). Karten starten im Bestand, nicht im Deck. Noch nicht vom Nutzer in Godot gegengeprüft.
7. **#14 Add damage-over-time status effects** (Gift/Brand/Blutung) – bewusst von #12 abgetrennter Fast-Follow für gekoppelte Modifikatoren, braucht ein neues Status-Effekt-System auf `Enemy.cs`.
8. ~~**#15 Let the player choose among multiple drawn cards on level-up**~~ – **umgesetzt** (siehe Kartenauswahl-Abschnitt oben: 2 Karten pro Level-up, Klick auf eine `CardView` entscheidet, nicht gewählte Karte wandert dauerhaft auf eine neue Ablage – `Deck.Discard`/`DrawHand(..., allowReshuffleFromDiscard: false)`). Daraus zwei neue Fast-Follow-Issues: **#21** (Karte mischt Ablage zurück ins Deck) und **#22** (Karte holt gezielt eine Karte aus der Ablage zurück), beide noch offen. Noch nicht vom Nutzer in Godot gegengeprüft.
9. **#21 Add a card that reshuffles the discard pile back into the draw pile** – Fast-Follow aus #15, macht die Ablage wieder nutzbar (komplett).
10. **#22 Add a card that lets the player pick a specific card from the discard pile** – Fast-Follow aus #15, gezielte Variante zu #21 (eine Karte statt aller).
11. **#4 Add Card Shop** – braucht Gold-Belohnungen (jetzt aus #2/#10 vorhanden, `PlayerWallet`) und einen volleren Kartenpool aus #12.
12. **#13 Add new characters** – Charakterinhalt, Voraussetzung für Gruppe & Charakter-Shop.
13. **#5 Add Group** – Party bis 4 Charaktere, braucht #13.
14. **#7 Add character death** – überschneidet sich stark mit #5 (Permadeath für Nicht-Hauptcharaktere), am besten zusammen mit #5 umsetzen statt als getrennten Schritt.
15. **#6 Add Character Shop** – braucht #13, #5 und das Shop-Muster aus #4.
16. **#8 Add stat overview after stage & dungeon** – Reporting-Capstone, braucht Gruppe (#5) und Stage/Dungeon-Struktur (#2/#3/#10) als Datengrundlage.
17. **#16 Add a gold-theft enemy type** – kleine Content-Ergänzung, blockiert nichts anderes.
18. **#20 Decide how to handle deck exhaustion** – geringe Priorität, tritt bei aktuellen Balance-Werten praktisch nicht auf (~Level 11).
19. **#17 Do a real balancing pass** – bewusst spät, erst wenn Gegner-/Karten-/Boss-Inhalt nicht mehr in Bewegung ist. `Arena.TestWaveCountOverride` vorher zurücksetzen.
20. **#18 Replace drawn circles with real sprites/animations** – Art-Pass, braucht Assets von außerhalb, sinnvoll erst wenn die Mechanik sich beruhigt hat.
21. **#19 Decide whether to delete the legacy turn-based combat code** – reine Aufräumarbeit, jederzeit möglich, keine Abhängigkeiten.

Godot-Physik/Kollisionslayer für die Arena (falls die reinen Distanzchecks irgendwann nicht mehr reichen, z. B. für Gegner-Ausweichverhalten untereinander) ist bewusst **kein eigenes Issue** – rein spekulativ, kein aktueller Auslöser.
