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
- Ordnerstruktur (umgesetzt):
  ```
  /scripts
    /character   (Stats, Charakterbogen-Logik: AbilityScores, PlayerCharacter, Enemy)
    /combat      (Würfel-Resolution: Dice; CombatEngine/LogTag/AttackResult/RollStamp/AbilityCheckResult gehören zum alten rundenbasierten Kampf, siehe Altlasten)
    /cards       (Kartendeck-Logik: CardDefinition + Karten, CardCatalog, Deck – jetzt Grundlage der Echtzeit-Fähigkeiten)
    /dungeon     (noch leer – künftige prozedurale Generierung)
    /meta        (PlayerCardCollection/PlayerWallet/DungeonRun – Kartenbesitz/Gold/laufender Dungeon-Fortschritt über Szenenwechsel/Runs hinweg)
  /scenes        (Hub/Arena/Player/EnemyGoblin/FloatingText/DeckScreen/CardView/Camp/Main + jeweiliges .cs als UI-/Ablauf-Glue, Godot-Konvention: Script liegt bei seiner Szene)
  ```
- Godot 4.7, `Godot.NET.Sdk` (.NET), `<Nullable>enable</Nullable>` in `PPRogueLite.csproj`. Fenster fest auf 1920×1080 in `project.godot` (`[display]`), Startszene `res://scenes/Hub.tscn` (`[application] run/main_scene`), globales Theme über `[gui] theme/custom`. Kein Autoload/Singleton-Mechanismus – Cross-Scene-State läuft ausschließlich über C#-`static`-Felder (siehe `PlayerCardCollection`).

## Technischer Aufbau (für neue Sessions: hier zuerst lesen)

Kompakte Referenz, damit eine neue Session ohne Gesprächsverlauf schnell versteht, wie der Code zusammenhängt – ergänzt die Ordnerstruktur oben um Verantwortlichkeiten und gelernte Godot-Fallstricke.

**Namespaces** (alle unter `PPRogueLite`):
- `PPRogueLite` (Root): Godot-Node-Scripts direkt unter `scenes/` – `Hub`, `Arena`, `Player`, `EnemyGoblin`, `FloatingText`, `CardView`, `DeckScreen`, `DeckStackView`, `Camp`, `Main` (Altlast).
- `PPRogueLite.Character`: `AbilityScores` (Ability-Enum + Modifier-Berechnung), `PlayerCharacter`, `Enemy` – reine Datenklassen, kein Godot-Bezug.
- `PPRogueLite.Combat`: `Dice` (statischer W20/WN-Roller, einziger RNG im Projekt) + Altlasten `CombatEngine`/`LogTag`/`AttackResult`/`AbilityCheckResult`/`RollStamp`.
- `PPRogueLite.Cards`: `CardDefinition` (abstract: Id/DisplayName/CardType/Description/RequirementText/IsExhaust/`Play(CombatEngine)`) + 5 konkrete Karten, `CardCatalog` (baut Start-/Bench-Deck, `AllCardTypes()`), `Deck` (Draw-/Discard-Pile, Shuffle, optional ohne Mischen via `shuffleOnCreate: false` – kein Godot-Bezug).
- `PPRogueLite.Meta`: `PlayerCardCollection` (static class, `DeckCards`/`BenchCards`-Listen), `PlayerWallet` (static class, `Gold`), `DungeonRun` (static class, Fortschritt eines laufenden Dungeons über Stages hinweg – siehe Dungeon-Struktur-Abschnitt unten).

**Wichtig zu verstehen – `CardDefinition.Play(CombatEngine)` ist Altlast:** Das war der Auslöse-Mechanismus des alten rundenbasierten Kampfs. Die Echtzeit-Arena nutzt ihn **nicht** – `Player.TriggerAbility` verdrahtet das Verhalten jeder Karte stattdessen fest per `switch` über `CardDefinition.Id` (siehe Echtzeit-Arena-Abschnitt unten). Beim Ergänzen neuer Karten also nicht `Play()` implementieren in der Annahme, das reiche – der Switch in `Player.cs` muss den neuen Id-Fall auch behandeln.

**Datenfluss Kartenbesitz → Run:** `PlayerCardCollection` ist eine statische Klasse (überlebt Szenenwechsel via CLR-Static-Feld-Lebensdauer, kein Autoload nötig). `DeckScreen` liest/schreibt sie direkt (×/+-Buttons verschieben Karteninstanzen zwischen `DeckCards`/`BenchCards`). `Player._Ready()` kopiert beim **ersten** Arena-Start eines Dungeons den aktuellen Stand von `DeckCards` in ein **laufeigenes** `new Deck(...)` – Deck-Änderungen wirken sich also erst auf den **nächsten** Dungeon aus, nie auf einen laufenden.

**Datenfluss Charakter-Fortschritt → nächste Stage:** Godot behält beim Szenenwechsel keinen Node-State (die alte `Player`-Instanz wird beim Verlassen der Arena zerstört). Damit HP/Level/XP/ausgerüstete Fähigkeiten/Nachziehstapel über mehrere Stages *desselben* Dungeons erhalten bleiben, schreibt `Player.SaveProgress()` sie vor jedem Szenenwechsel in `DungeonRun` (aufgerufen von `Arena.CompleteStage()`); `Player._Ready()` liest sie beim Start der nächsten Stage über `DungeonRun.HasProgress` wieder ein, statt einen frischen Charakter zu bauen. Siehe Dungeon-Struktur-Abschnitt unten für den vollen Ablauf.

**Szenen/Node-Struktur** (Script liegt immer neben seiner `.tscn`, Godot-Konvention):
- `Hub.tscn` (Startszene, "Taverne"): `Control` mit 4 Buttons in einem Papier-Panel + Gold-Anzeige; `Hub.cs` verdrahtet nur die ersten beiden (`GetTree().ChangeSceneToFile("res://scenes/Arena.tscn"|"res://scenes/DeckScreen.tscn")`, "Dungeon betreten" ruft zusätzlich `DungeonRun.Start()`).
- `Arena.tscn`: `Node2D`-Root (`Arena.cs` = Orchestrator) mit Kind-Node `Player` (Instanz von `Player.tscn`), `WaveTransitionTimer`, und `HUD` (`CanvasLayer`) mit `MarginContainer → VBoxContainer` (AbilityBar, HP/XP-Labels+Bars, StageLabel, WaveLabel, SurvivalLabel, OutcomeLabel, ContinueButton) plus separatem `LevelUpLayer` (`MarginContainer`, initial leer, wird zur Laufzeit von `Arena.cs` mit dem 3-Spalten-Level-up-Screen befüllt und wieder geleert).
- `Player.tscn`: `Node2D`, Script `Player.cs` (WASD-Bewegung, Fähigkeiten-Liste, XP/Level, kein Sprite – `_Draw()`).
- `EnemyGoblin.tscn`: `Node2D`, Script `EnemyGoblin.cs` (verfolgt Spieler, greift bei Kontakt an, `_Draw()`).
- `FloatingText.tscn`: `Node2D` mit `Label`-Kind, Script `FloatingText.cs` (Tween-Animation, self-destruct via `QueueFree`).
- `DeckScreen.tscn`: zwei Spalten (Im Deck / Nicht im Deck), je ein `VBoxContainer` [HeaderLabel, PanelContainer→ScrollContainer→GridContainer] mit `CardView`-Instanzen.
- `CardView.tscn`: wiederverwendbare Kartenansicht (Panel + Labels + ActionButton), genutzt in `DeckScreen` UND im Arena-Level-up-Screen.
- `Camp.tscn` ("Lager"): `Control`, gleiche Papier-Menü-Struktur wie `Hub.tscn` (Titel/Untertitel/Gold-Anzeige + zentriertes Menüpanel), zwei Buttons ("Nächste Stage betreten" / "Dungeon beenden"). Zwischenstopp zwischen zwei Stages *innerhalb* eines Dungeons – nicht zu verwechseln mit dem Hub/der Taverne, die nur *zwischen* zwei Dungeons erreichbar ist.
- `Main.tscn`/`Main.cs`: Altlast des alten rundenbasierten Kampfs, von keinem Menü mehr erreicht – siehe "Altlasten" unten, nicht ohne Rücksprache löschen.

**Godot-C#-Muster/Learnings** (gegen echte Bugs erarbeitet – beim Weiterbauen beachten):
- **Node-Lifecycle:** `_Ready()` feuert erst NACH `AddChild()` in den lebenden SceneTree, nicht direkt bei `PackedScene.Instantiate()`. Reihenfolge beim dynamischen Erzeugen immer: `Instantiate()` → Properties setzen → `AddChild()` → erst dann Methoden wie `Populate()` aufrufen, die auf in `_Ready()` gesetzte Felder zugreifen (hat einen echten NullReferenceException-Bug verursacht).
- **Kein Drag & Drop:** `_CanDropData`/`_DropData` funktionierte im echten Editor nicht (vermutlich falsche Bubbling-Annahme). Verschieben/Interaktion läuft seitdem über explizite Buttons + Events (`ActionClicked`, `Clicked`).
- **Kein Godot-Physik-Kollisionssystem:** bewusst vermieden (ungetestete Layer/Masken) – Nähe/Reichweite immer per `Vector2.DistanceTo()`-Handrechnung (`Player`/`EnemyGoblin`).
- **Keine globale Pause** (`GetTree().Paused`): stattdessen manuelle `SetDisabled(bool)`-Flags auf `Player`/`EnemyGoblin` + `Timer.Stop()`, gesteuert über `Arena.SetWorldPaused()`.
- **Kein Input-Map:** WASD wird direkt per `Input.IsPhysicalKeyPressed(Key.W/A/S/D)` abgefragt statt über `[input]`-Actions in `project.godot`.
- **Gruppen statt Referenzlisten:** `AddToGroup("player"/"enemies")` + `GetTree().GetNodesInGroup(...)` für Player↔Enemy-Discovery.
- **Async/Await für Bestätigungsdialoge:** `await ToSignal(button, Button.SignalName.Pressed)` pausiert eine Methode bis zum Klick – genutzt sowohl im alten Popup als auch im Level-up-"Weiter"-Flow.
- **Tween-API:** `CreateTween().SetParallel(true).TweenProperty(node, "position:y", ziel, dauer)` für einfache Animationen (`FloatingText`), Sub-Properties per String-Pfad in snake_case (`"position:y"`, `"modulate:a"`).
- **Theme:** eine globale Theme-Resource (`theme/game_theme.tres`) über `project.godot` `[gui] theme/custom`, mit benannten `theme_type_variation`-Varianten (z. B. `CardPanel`, `DeskLabel`, `CardActionButton`) statt Styling pro Node.
- **Layout ohne natives "space-between":** `HBoxContainer` kennt das nicht – wird über leere `Control`-Spacer-Nodes mit `SizeFlags.Fill|Expand` zwischen fixen Spalten nachgebaut (siehe `Arena.OnAbilityGained`).

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

Neue Szenen `scenes/Arena.tscn` + `scenes/Player.tscn` + `scenes/EnemyGoblin.tscn` + `scenes/FloatingText.tscn` (jeweils mit zugehörigem `.cs`), plus `scenes/DeckStackView.cs` (reiner Code, keine eigene `.tscn` nötig):

- **Player** (`Player.cs`, `Node2D`, kein Physik-Kollisionssystem – Bewegung/Reichweite laufen über einfache Distanzberechnung, um ungetestete Kollisions-Layer/Masken zu vermeiden): WASD-Bewegung (`Input.IsPhysicalKeyPressed`, direkt abgefragte Tasten statt Input-Map-Actions), begrenzt auf den Viewport. Visuell ein per `_Draw()` gezeichneter Kreis (Messingfarbe) plus ein halbtransparenter Ring in Angriffsreichweite um den Charakter – keine Sprite-Assets vorhanden.
- **Fähigkeiten statt Handkarten:** Player hält eine Liste aktiver Fähigkeiten (`ActiveAbility`: `CardDefinition` + eigener Cooldown-Timer), die jede für sich automatisch auslöst, sobald ihr Cooldown abläuft (`Player._Process` → `UpdateAbilities`). Start jedes Runs: fest mit "Hieb" ausgerüstet (`new HiebCard()`, kein Zug aus dem Deck nötig – Sicherheitsnetz gegen einen Run ohne Angriff).
- **Fähigkeiten-Leiste im HUD:** oben links zeigt `Arena` (`AbilityBar`, `HBoxContainer`) für jeden **unterschiedlichen** aktiven Kartentyp ein Badge (Name + Stückzahl, falls mehrfach gezogen – z. B. "Hieb ×2" – plus Cooldown-Balken, der sich füllt und beim Auslösen der Fähigkeit zurückspringt – `Player.GetCooldownProgress(cardId)`, pro Frame ausgelesen). Neue Badges kommen von links nach rechts hinzu, sobald ein bisher unbekannter Kartentyp gezogen wird (`Player.NewAbilityTypeUnlocked`, unterscheidet sich von `AbilityGained`, das bei jeder Ziehung inkl. Duplikaten feuert und den Zähler aktualisiert). Bei mehreren Instanzen desselben Kartentyps zeigt der Cooldown-Balken nur die erste/primäre Instanz.
- **XP-Balken im HUD** (`XpLabel`/`XpBar`, unter der HP-Anzeige): zeigt Fortschritt zum nächsten Level (`Player.Xp`/`XpToNextLevel`).
- **Deck-Anbindung:** Beim `_Ready()` baut sich `Player` ein **laufeigenes** `Deck` aus `PlayerCardCollection.DeckCards` (`new Deck(PlayerCardCollection.DeckCards)` – kopiert nur die Zusammensetzung, verändert die Sammlung im Deck-Screen nicht). Bei jedem Level-up (`GrantXp`/`LevelUp`) wird `_runDeck.DrawHand(1)` gezogen und dauerhaft als neue `ActiveAbility` ausgerüstet. Auswahl zwischen mehreren gezogenen Karten (statt automatisch die eine gezogene auszurüsten) ist **noch nicht umgesetzt** (Nutzer nannte das als mögliche Erweiterung).
- **Verhalten pro Karte** ist in `Player.TriggerAbility` fest verdrahtet (switch über `CardDefinition.Id`, kein neuer generischer "Echtzeit-Play"-Mechanismus auf `CardDefinition` selbst, um die reine Datenklasse nicht an Godot-Typen zu koppeln): Hieb/Wuchtschlag = Nahkampfangriff auf den nächsten Gegner in Reichweite (verdeckter W20 + STR-Mod gegen RK, Schadenswürfel, Vorteil möglich, Kritischer Treffer bei natürlicher 20 = doppelter Schaden), Parade = temporärer RK-Bonus-Puls, Finte = setzt "Vorteil beim nächsten Treffer", Atem holen = Selbstheilung – jeweils mit eigenem Cooldown.
- **XP/Level:** 1 XP pro getötetem Gegner (`EnemyGoblin.TakeDamage` → `Player.GrantXp`), 2 XP pro Level (Test-Balance-Wert, damit sich beim Testen mit ~2 Kills pro Level-up schnell was tut – ursprünglich 5, bewusst gesenkt). Bei einem Level-up **pausiert die Runde**: `Arena.SetWorldPaused(true)` deaktiviert Player (`SetDisabled`), alle Gegner (`SetDisabled` über die `"enemies"`-Gruppe) und den Spawn-Timer. Der Level-up-Screen nutzt dabei die **volle Bildschirmbreite** (an einer Nutzer-Skizze orientiert, noch kein fertiges Design) – `LevelUpLayer` ist ein `MarginContainer` (nicht mehr `CenterContainer`), die drei Spalten werden über Fill/Expand-`Control`-Spacer zwischen ihnen auf die volle Breite verteilt (Godots `HBoxContainer` kennt kein natives "space-between"):
  - **Links:** zwei Kartenstapel-Übersichten ("Nachziehstapel" / "Bereits gezogen"), jeweils mit einer schrumpfenden Stapel-Grafik (`scenes/DeckStackView.cs`, eigener `Control` mit `_Draw()` – zeichnet eine Karten-Rückseite pro noch vorhandener Karte, leicht versetzt, wird also sichtbar kleiner) plus allen 5 bekannten Kartentypen mit Stückzahl (`CardCatalog.AllCardTypes()`), bei 0 ausgegraut. Zeigt bewusst nur Summen pro Typ, nicht die tatsächliche (gemischte) Reihenfolge des Nachziehstapels. "Bereits gezogen" entspricht den aktuell ausgerüsteten Fähigkeiten (`Player.CountEquipped`) – im Echtzeit-Modus gibt es keinen klassischen Ablagestapel, gezogene Karten werden ja dauerhaft ausgerüstet statt abgelegt.
  - **Mitte:** die neu gezogene Karte (`CardView`) + "Weiter"-Button, erst nach Klick (`await ToSignal(...)`) geht's weiter.
  - **Rechts:** Charakterbogen (Bild-Platzhalter, Name, Klasse, Rüstungsklasse, sechs Attribute) im Format "Wert (Basiswert Delta)" – Delta aktuell fast immer +0 (nur die Rüstungsklasse kann sich durch Parade kurzzeitig erhöhen), Format aber bereits vorbereitet für künftige Boni (grün) / Mali (rot) durch Ereignisse/Ausrüstung.
- **Gegner** (`EnemyGoblin.cs`): läuft direkt auf die Spielerposition zu (kein Pathfinding), greift bei Kontakt automatisch an (verdeckter Wurf, gleiche Werte wie bisher bis auf die RK: Höhlengoblin RK **8** [Test-Balance-Wert, bewusst niedriger als die kanonische RK 13 aus Prototyp/Deck-Screen, damit Treffer/XP beim Testen der Arena schneller kommen], 12 HP, Angriffsbonus +4, 1W6+2), zeigt einen kleinen Lebensbalken über sich (per `_Draw()`, aktualisiert bei jedem Treffer über `QueueRedraw()`).
- **Wellen/Stage-Struktur** (`Arena.BeginWave`/`CheckWaveCleared`, Umsetzung von GitHub-Issue #2): eine Stage besteht aus 10 Wellen, Welle N spawnt N Gegner auf einmal (Welle 1 = 1, Welle 2 = 2, ... Welle 10 = 10 – Test-Balance-Wert). Die nächste Welle startet erst, wenn die aktuelle vollständig besiegt ist (`CheckWaveCleared` pollt pro Frame `GetTree().GetNodesInGroup("enemies").Count == 0`, kein Event von `EnemyGoblin` nötig), dann kurze Pause über `WaveTransitionTimer` (`Arena.tscn`, one-shot, 1,5 s) bevor die nächste Welle spawnt. Aktuelle Welle wird im HUD angezeigt (`WaveLabel`, "Welle: N / 10"). Nach Welle 10 endet die Stage als **Sieg** (siehe HUD unten) – vorher lief die Arena endlos ohne Sieg-Bedingung, das ist jetzt abgelöst.
- **Feedback statt Popup:** `FloatingText.cs` (kurzer, aufsteigender/verblassender Text) zeigt Schaden/"Verfehlt"/Heilung/Buff-Namen direkt am Ort des Geschehens – ersetzt das alte Log-/Popup-System vollständig.
- **HUD** (`Arena.tscn`, `CanvasLayer`): Fähigkeiten-Leiste, HP-Balken/-Text, XP-Balken/-Text, Stage-Anzeige, Wellen-Anzeige, Überlebenszeit, Ergebnis-Text (`OutcomeLabel`, "Niederlage"/"Stage N von 5 abgeschlossen! +N Gold"/"Dungeon abgeschlossen! +N Gold") und ein Button mit dynamischem Text/Ziel (`ContinueButton`, gesteuert über `Arena.FinishRun(text, buttonText, targetScene)` – "Weiter zum Lager" → `Camp.tscn` bei einer Nicht-Schluss-Stage, sonst "Zurück zur Taverne" → `Hub.tscn`).
- **Gold-Belohnung:** `PPRogueLite.Meta.PlayerWallet` (static class, gleiches Muster wie `PlayerCardCollection`) hält `Gold` prozessweit. Bei Stage-Abschluss gibt's einen festen Betrag (`Arena.GoldReward = 25`, Test-Balance-Wert) – noch nirgends ausgegeben (folgt mit Card Shop/Issue #4). Wird im Hub und im Lager angezeigt.

**Bewusste Vereinfachungen dieser ersten Version** (nicht vergessen, wenn's ans Polishing geht): keine Godot-Physik/Kollisionslayer (nur Distanzchecks, Gegner können sich gegenseitig überlappen), keine Auswahl zwischen mehreren gezogenen Karten beim Level-up, kein Sprite/Animationen (nur gezeichnete Kreise/Formen), Cooldown-Balken bei doppelten Kartentypen zeigt nur die erste Instanz. Ausgerüstete Fähigkeiten starten bei jeder neuen Stage mit vollem Cooldown neu (kein Versuch, den exakten Timer-Stand über den Szenenwechsel zu retten).

Bewegung/Angriffe/Ability-Leiste/Cooldowns/XP/Level-up-Pause/Deck-Stapel-Grafik/Game-Over-Flow vom Nutzer in Godot getestet, funktioniert (iterativ über mehrere Runden verfeinert, siehe Balance-Werte oben: Goblin-RK 8, 2 XP/Level). Das Wellen-/Stage-System (Issue #2) und die Mehrfach-Stage-Anbindung (Issue #3, siehe Dungeon-Struktur-Abschnitt unten) sind neu und **noch nicht in der echten Godot-Umgebung gegengeprüft**.

## Dungeon-Struktur (bereits aufgesetzt, Issue #3)

Ein Dungeon besteht aus `DungeonRun.TotalStages` = 5 Stages hintereinander (jede Stage = ein voller Arena-Durchlauf mit 10 Wellen, siehe oben). Neuer Zustand `PPRogueLite.Meta.DungeonRun` (static class, gleiches Muster wie `PlayerCardCollection`/`PlayerWallet`) hält den Fortschritt über die Szenenwechsel Arena ↔ Lager hinweg fest, da eine neue Stage eine neue `Arena`-Szene (und damit eine neue `Player`-Instanz) bedeutet:

- **Start eines Dungeons:** `Hub.OnEnterDungeonPressed` ruft `DungeonRun.Start()` (setzt `CurrentStage = 1`, `HasProgress = false`) und wechselt zu `Arena.tscn`. `Player._Ready()` sieht `HasProgress == false` und baut wie bisher einen frischen Charakter (volles HP, Level 1, nur Hieb ausgerüstet, frisches Deck aus `PlayerCardCollection.DeckCards`).
- **Stage-Abschluss, Dungeon geht weiter** (`Arena.CompleteStage`, `DungeonRun.CurrentStage < TotalStages`): Gold gutschreiben, `Player.SaveProgress()` schreibt HP/XP/Level/Nachziehstapel/ausgerüstete Fähigkeiten in `DungeonRun` (setzt `HasProgress = true`), `DungeonRun.AdvanceStage()` erhöht `CurrentStage`, dann `Arena.FinishRun(...)` mit Button "Weiter zum Lager" → `Camp.tscn`.
- **Lager** (`Camp.tscn`/`Camp.cs`, "Lager"): zeigt "Stage N von 5 abgeschlossen" und den aktuellen Gold-Stand. Zwei Buttons: **"Nächste Stage betreten"** → `Arena.tscn` (neue `Player`-Instanz liest `DungeonRun.HasProgress == true` und stellt HP/XP/Level/Deck/Fähigkeiten wieder her, statt frisch zu starten – `Deck`-Konstruktor mit `shuffleOnCreate: false`, damit der gespeicherte Nachziehstapel nicht neu gemischt wird); **"Dungeon beenden"** → `DungeonRun.End()` + zurück zu `Hub.tscn` (bereits verdientes Gold bleibt erhalten, da es schon pro Stage direkt in `PlayerWallet` landet – "beenden" verliert nichts, es bricht nur zukünftige Stages ab).
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
- **10-Karten-Regel:** Das Deck darf beim Bearbeiten größer oder kleiner als 10 werden, die Anzahl über der Deck-Spalte wird bei mehr als 10 Karten rot. "Zurück zum Hub" ist deaktiviert, solange das Deck **nicht genau 10 Karten** enthält; ein Hinweistext unter den Spalten erklärt warum.
- "Zurück zum Hub" wechselt zurück zu `scenes/Hub.tscn` (nur klickbar, wenn genau 10 Karten im Deck sind).

Datengrundlage ist weiterhin `PPRogueLite.Meta.PlayerCardCollection` (`scripts/meta/PlayerCardCollection.cs`) – eine statische Klasse, deren Felder den Prozess über Szenenwechsel hinweg überleben. Sie hält zwei Listen (`DeckCards`, `BenchCards`), befüllt über `CardCatalog.BuildWarriorStartingDeck()` (10 Karten) und `CardCatalog.BuildWarriorBenchCards()` (7 zusätzliche Karten: 2× Hieb, 2× Wuchtschlag, 1× Parade, 1× Finte, 1× Atem holen).

**Wiederverwendbare Kartenansicht `scenes/CardView.tscn`/`CardView.cs`:** zeigt Typ/Name/Beschreibung/Anforderung direkt auf der Karte (keine Hover-Info nötig) und optional eine Stückzahl. Aufbau: äußerer `Control` mit einem `PanelContainer` (Kartentext, `CardPanel`-Theme-Variante) und einem kleinen `Button` oben rechts (`ActionButton`, `CardActionButton`-Theme-Variante, per `ShowAction("×"/"+")` sichtbar geschaltet, Klick löst `ActionClicked` aus). Wird auch in der Echtzeit-Arena wiederverwendet, um gezogene Karten beim Level-up anzuzeigen (dort ohne Action-Badge, reine Anzeige).

**Jetzt verbunden:** Die Echtzeit-Arena (`Player.cs`) liest `PlayerCardCollection.DeckCards` beim Run-Start (kopiert die Zusammensetzung in ein laufeigenes `Deck`) – Karten, die im Deck-Screen verschoben werden, wirken sich also auf den **nächsten** Run aus (siehe Echtzeit-Arena-Abschnitt oben). Der alte rundenbasierte Kampf (`Main.cs`) hatte diese Verbindung nie bekommen und ist inzwischen ohnehin nicht mehr erreichbar (siehe Altlasten oben).

Vom Nutzer in Godot getestet, funktioniert (×/+-Buttons, Stückzahl-Anzeige, 10-Karten-Sperre).

## Offene Punkte / nächste Schritte

**GitHub Issues sind jetzt das Backlog** für größere Features (Repo `Sventie/P-P-Rogue-Lite`, Issues #2–#13). Gemeinsam mit dem Nutzer erarbeitete Abarbeitungsreihenfolge (nach technischen Abhängigkeiten, nicht nach Issue-Nummer):

1. ~~**#2 Add Stage logic**~~ – **umgesetzt** (siehe Echtzeit-Arena-Abschnitt oben: 10 Wellen pro Stage, Welle N = N Gegner, Gold-Belohnung bei Abschluss). Noch nicht vom Nutzer in Godot gegengeprüft.
2. ~~**#3 Add Dungeon logic**~~ – **umgesetzt** (siehe Dungeon-Struktur-Abschnitt oben: 5 Stages pro Dungeon, neues Lager `Camp.tscn` als Zwischenstopp *innerhalb* eines Dungeons, Taverne/Hub nur noch *zwischen* Dungeons, Charakter-Fortschritt über `DungeonRun` erhalten). Noch nicht vom Nutzer in Godot gegengeprüft.
3. **#10 Add Dungeonpath** – verzweigte Node-Routenauswahl statt linearer Stage-Kette aus #3, bewusst direkt danach, um die lineare Verkettung nicht erst zu bauen und dann zu verwerfen.
4. **#9 Add different enemies** (ranged/tank/mage/thief) – sinnvoll, sobald es echte Stage-/Pfad-Vielfalt zum Befüllen gibt.
5. **#11 Add boss fight in stage 10** – braucht Stage 10 aus #10 und die Gegner-Designsprache aus #9.
6. **#12 add new cards** (Angriffs-/Verteidigungs-/Fähigkeits-/Modifikator-Karten) – Karteninhalt vertiefen.
7. **#4 Add Card Shop** – braucht Gold-Belohnungen (jetzt aus #2 vorhanden, `PlayerWallet`) und einen volleren Kartenpool aus #12.
8. **#13 Add new characters** – Charakterinhalt, Voraussetzung für Gruppe & Charakter-Shop.
9. **#5 Add Group** – Party bis 4 Charaktere, braucht #13.
10. **#7 Add character death** – überschneidet sich stark mit #5 (Permadeath für Nicht-Hauptcharaktere), am besten zusammen mit #5 umsetzen statt als getrennten Schritt.
11. **#6 Add Character Shop** – braucht #13, #5 und das Shop-Muster aus #4.
12. **#8 Add stat overview after stage & dungeon** – Reporting-Capstone, braucht Gruppe (#5) und Stage/Dungeon-Struktur (#2/#3) als Datengrundlage.

Sonstige offene Punkte, unabhängig vom Issue-Backlog:

- **Deck-Erschöpfung:** Ist das Deck leer (nach ca. Level 11 bei 10 Karten), liefert `LevelUp()` einfach keine neue Fähigkeit mehr (stiller No-op, `drawn.Count == 0` → return). Bewusst nicht behoben, kein akuter Bedarf – möglicher Ansatz später: Nachziehstapel aus ausgerüsteten Karten neu mischen, oder bei XP-Überschuss einfach nichts mehr passieren lassen.
- Auswahl zwischen mehreren gezogenen Karten beim Level-up (aktuell wird automatisch die eine gezogene Karte ausgerüstet).
- Balancing von Karten-Synergien und Progressionskurve (XP-pro-Level, Fähigkeiten-Cooldowns, Wellengröße, Gold-Belohnung – aktuell grobe Testwerte, bewusst leicht gestellt zum schnellen Iterieren, noch nicht auf "echtes" Balancing hin geprüft).
- Echte Sprites/Animationen statt gezeichneter Kreise/Formen.
- Godot-Physik/Kollisionslayer für die Arena einführen, falls die reinen Distanzchecks nicht mehr reichen (z. B. für Gegner-Ausweichverhalten untereinander).
- Entscheidung, ob/wann die Altlasten (alter rundenbasierter Kampf, siehe eigener Abschnitt) endgültig gelöscht werden.
