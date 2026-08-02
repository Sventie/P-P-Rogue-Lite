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
    /meta        (PlayerCardCollection – Kartenbesitz über Szenenwechsel/Runs hinweg)
  /scenes        (Hub/Arena/Player/EnemyGoblin/FloatingText/DeckScreen/CardView/Main + jeweiliges .cs als UI-/Ablauf-Glue, Godot-Konvention: Script liegt bei seiner Szene)
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

- **"Dungeon betreten"** – **funktional**: wechselt per `GetTree().ChangeSceneToFile()` zu `scenes/Arena.tscn` und startet damit die Echtzeit-Arena (siehe eigener Abschnitt unten). Zeigte früher auf `scenes/Main.tscn` (alter rundenbasierter Kampf) – siehe Pivot oben.
- **"Karten managen"** – **funktional**: wechselt zu `scenes/DeckScreen.tscn` (siehe eigener Abschnitt unten).
- **"Gruppe managen"** – *noch ohne Funktion (UI-Platzhalter)*. Geplant: alle besessenen Charaktere als Charakterkarten; Anklicken einer Karte öffnet das Charakterbogen-Sheet (Bezug zur Roster-/Gruppen-Planung in der Meta-Ebene oben).
- **"Mit Loot entkommen"** – *noch ohne Funktion (UI-Platzhalter)*. Geplant: Weg, einen Run kontrolliert/sicher zu beenden (Loot behalten statt Risiko eines Wipes).

Bei Niederlage in der Arena (siehe unten) erscheint ein **"Zurück zum Hub"**-Button und wechselt per Szenenwechsel zurück zu `scenes/Hub.tscn`. Jeder erneute Einstieg über "Dungeon betreten" baut den Run komplett neu auf (frischer Charakter/Deck-Ziehung/Gegner-Spawns).

**Noch nicht in der echten Godot-Umgebung gegengeprüft.**

## Echtzeit-Arena (bereits aufgesetzt, löst den alten Main.tscn-Kampf ab)

Neue Szenen `scenes/Arena.tscn` + `scenes/Player.tscn` + `scenes/EnemyGoblin.tscn` + `scenes/FloatingText.tscn` (jeweils mit zugehörigem `.cs`):

- **Player** (`Player.cs`, `Node2D`, kein Physik-Kollisionssystem – Bewegung/Reichweite laufen über einfache Distanzberechnung, um ungetestete Kollisions-Layer/Masken zu vermeiden): WASD-Bewegung (`Input.IsPhysicalKeyPressed`, direkt abgefragte Tasten statt Input-Map-Actions), begrenzt auf den Viewport. Visuell ein per `_Draw()` gezeichneter Kreis (Messingfarbe) plus ein halbtransparenter Ring in Angriffsreichweite um den Charakter – keine Sprite-Assets vorhanden.
- **Fähigkeiten statt Handkarten:** Player hält eine Liste aktiver Fähigkeiten (`ActiveAbility`: `CardDefinition` + eigener Cooldown-Timer), die jede für sich automatisch auslöst, sobald ihr Cooldown abläuft (`Player._Process` → `UpdateAbilities`). Start jedes Runs: fest mit "Hieb" ausgerüstet (`new HiebCard()`, kein Zug aus dem Deck nötig – Sicherheitsnetz gegen einen Run ohne Angriff).
- **Fähigkeiten-Leiste im HUD:** oben links zeigt `Arena` (`AbilityBar`, `HBoxContainer`) für jeden **unterschiedlichen** aktiven Kartentyp ein Badge (Name + Stückzahl, falls mehrfach gezogen – z. B. "Hieb ×2" – plus Cooldown-Balken, der sich füllt und beim Auslösen der Fähigkeit zurückspringt – `Player.GetCooldownProgress(cardId)`, pro Frame ausgelesen). Neue Badges kommen von links nach rechts hinzu, sobald ein bisher unbekannter Kartentyp gezogen wird (`Player.NewAbilityTypeUnlocked`, unterscheidet sich von `AbilityGained`, das bei jeder Ziehung inkl. Duplikaten feuert und den Zähler aktualisiert). Bei mehreren Instanzen desselben Kartentyps zeigt der Cooldown-Balken nur die erste/primäre Instanz.
- **XP-Balken im HUD** (`XpLabel`/`XpBar`, unter der HP-Anzeige): zeigt Fortschritt zum nächsten Level (`Player.Xp`/`XpToNextLevel`).
- **Deck-Anbindung:** Beim `_Ready()` baut sich `Player` ein **laufeigenes** `Deck` aus `PlayerCardCollection.DeckCards` (`new Deck(PlayerCardCollection.DeckCards)` – kopiert nur die Zusammensetzung, verändert die Sammlung im Deck-Screen nicht). Bei jedem Level-up (`GrantXp`/`LevelUp`) wird `_runDeck.DrawHand(1)` gezogen und dauerhaft als neue `ActiveAbility` ausgerüstet. Auswahl zwischen mehreren gezogenen Karten (statt automatisch die eine gezogene auszurüsten) ist **noch nicht umgesetzt** (Nutzer nannte das als mögliche Erweiterung).
- **Verhalten pro Karte** ist in `Player.TriggerAbility` fest verdrahtet (switch über `CardDefinition.Id`, kein neuer generischer "Echtzeit-Play"-Mechanismus auf `CardDefinition` selbst, um die reine Datenklasse nicht an Godot-Typen zu koppeln): Hieb/Wuchtschlag = Nahkampfangriff auf den nächsten Gegner in Reichweite (verdeckter W20 + STR-Mod gegen RK, Schadenswürfel, Vorteil möglich, Kritischer Treffer bei natürlicher 20 = doppelter Schaden), Parade = temporärer RK-Bonus-Puls, Finte = setzt "Vorteil beim nächsten Treffer", Atem holen = Selbstheilung – jeweils mit eigenem Cooldown.
- **XP/Level:** 1 XP pro getötetem Gegner (`EnemyGoblin.TakeDamage` → `Player.GrantXp`), 5 XP pro Level (grober erster Balance-Wert, ungetestet). Bei einem Level-up **pausiert die Runde**: `Arena.SetWorldPaused(true)` deaktiviert Player (`SetDisabled`), alle Gegner (`SetDisabled` über die `"enemies"`-Gruppe) und den Spawn-Timer. Der Level-up-Screen zeigt dabei drei Spalten (an einer Nutzer-Skizze orientiert, noch kein fertiges Design):
  - **Links:** zwei Kartenstapel-Übersichten ("Nachziehstapel" / "Bereits gezogen") – alle 5 bekannten Kartentypen mit Stückzahl (`CardCatalog.AllCardTypes()`), bei 0 ausgegraut. Zeigt bewusst nur Summen pro Typ, nicht die tatsächliche (gemischte) Reihenfolge des Nachziehstapels. "Bereits gezogen" entspricht den aktuell ausgerüsteten Fähigkeiten (`Player.CountEquipped`) – im Echtzeit-Modus gibt es keinen klassischen Ablagestapel, gezogene Karten werden ja dauerhaft ausgerüstet statt abgelegt.
  - **Mitte:** die neu gezogene Karte (`CardView`) + "Weiter"-Button, erst nach Klick (`await ToSignal(...)`) geht's weiter.
  - **Rechts:** Charakterbogen (Bild-Platzhalter, Name, Klasse, Rüstungsklasse, sechs Attribute) im Format "Wert (Basiswert Delta)" – Delta aktuell fast immer +0 (nur die Rüstungsklasse kann sich durch Parade kurzzeitig erhöhen), Format aber bereits vorbereitet für künftige Boni (grün) / Mali (rot) durch Ereignisse/Ausrüstung.
- **Gegner** (`EnemyGoblin.cs`): läuft direkt auf die Spielerposition zu (kein Pathfinding), greift bei Kontakt automatisch an (verdeckter Wurf, gleiche Werte wie bisher: Höhlengoblin RK 13, 12 HP, Angriffsbonus +4, 1W6+2), zeigt einen kleinen Lebensbalken über sich (per `_Draw()`, aktualisiert bei jedem Treffer über `QueueRedraw()`). `Arena.cs` spawnt alle 1,5 s einen neuen Gegner an einem zufälligen Punkt am Bildschirmrand – **keine Schwierigkeitssteigerung über die Zeit**, das ist ein bewusst offener erster Wurf.
- **Feedback statt Popup:** `FloatingText.cs` (kurzer, aufsteigender/verblassender Text) zeigt Schaden/"Verfehlt"/Heilung/Buff-Namen direkt am Ort des Geschehens – ersetzt das alte Log-/Popup-System vollständig.
- **HUD** (`Arena.tscn`, `CanvasLayer`): Fähigkeiten-Leiste, HP-Balken/-Text, XP-Balken/-Text, Überlebenszeit, "Zurück zum Hub" (erscheint erst bei Niederlage). Kein explizites Sieg-Ziel – der Run läuft, bis der Spieler stirbt (klassische Vampire-Survivors-Struktur).

**Bewusste Vereinfachungen dieser ersten Version** (nicht vergessen, wenn's ans Polishing geht): keine Godot-Physik/Kollisionslayer (nur Distanzchecks, Gegner können sich gegenseitig überlappen), keine Schwierigkeits-/Spawnrate-Steigerung über die Zeit, keine Auswahl zwischen mehreren gezogenen Karten beim Level-up, kein Sprite/Animationen (nur gezeichnete Kreise/Formen), Cooldown-Balken bei doppelten Kartentypen zeigt nur die erste Instanz.

**Noch nicht in der echten Godot-Umgebung gegengeprüft** – das gilt hier besonders, da es die erste Echtzeit-/Bewegungs-Logik im Projekt ist (bisher nur UI-lastige Szenen).

**Nächster Schritt:** Nutzer testet Bewegung/Angriffe/Level-up-Ziehungen/Game-Over-Flow in Godot. Danach mögliche Folgeschritte: Schwierigkeitskurve, Auswahl zwischen mehreren gezogenen Karten, echte Sprites, Balancing.

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

**Noch nicht in der echten Godot-Umgebung gegengeprüft.**

**Nächster Schritt:** Nutzer testet die ×/+-Buttons, Stückzahl-Anzeige und die 10-Karten-Sperre in Godot. Danach mögliche Folgeschritte: Kartenshop; "Gruppe managen".

## Offene Punkte

- Konkrete Klassen über den Krieger hinaus definieren.
- Dungeon-Generierungsalgorithmus (Layout, Encounter-Verteilung) – bisher nur eine einzelne Arena, keine Struktur mehrerer Räume/Encounter.
- Balancing von Karten-Synergien und Progressionskurve (XP-pro-Level, Fähigkeiten-Cooldowns, Gegner-Spawnrate – aktuell nur grobe erste Werte, ungetestet).
- Konkretes Reliquien/Bonus-System ausarbeiten.
- Schwierigkeitssteigerung über die Zeit in der Arena (aktuell konstante Spawnrate).
- Auswahl zwischen mehreren gezogenen Karten beim Level-up (aktuell wird automatisch die eine gezogene Karte ausgerüstet).
- Godot-Physik/Kollisionslayer für die Arena einführen, falls die reinen Distanzchecks nicht mehr reichen (z. B. für Gegner-Ausweichverhalten untereinander).
- Entscheidung, ob/wann die Altlasten (alter rundenbasierter Kampf, siehe eigener Abschnitt) endgültig gelöscht werden.
