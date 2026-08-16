namespace PPRogueLite.Meta;

using System.Collections.Generic;
using System.Linq;
using PPRogueLite.Character;

/// <summary>
/// Charakterbesitz + Gruppenzusammenstellung über Szenenwechsel/Runs hinweg
/// (statisches Feld, gleiches Muster wie PlayerCardCollection/PlayerWallet -
/// Godot behält beim Szenenwechsel keinen Node-State).
///
/// Alle 4 Archetypen aus CharacterClassCatalog sind von Start an im Besitz
/// (Issue #5). Der Charakter-Shop (Issue #6) kann das Roster darüber hinaus
/// erweitern - auch um weitere Exemplare einer bereits besessenen Klasse
/// (z. B. ein zweiter Bogenschütze), siehe AddCharacter. Leader ist fest
/// der ERSTE Krieger-Eintrag (Hauptcharakter, weiterhin von Player.cs
/// gesteuert) - SelectedCompanions hält bis zu MaxCompanions zusätzliche
/// Charaktere, die vor dem nächsten Dungeon frei neu zusammengestellt
/// werden können; die zuletzt gewählte Gruppe bleibt als Vorschlag stehen
/// (kein automatisches Zurücksetzen), tote Gefährten werden über MarkDead
/// sofort entfernt.
/// </summary>
public static class PlayerCharacterCollection
{
    public const int MaxCompanions = 3;

    public static List<OwnedCharacter> Roster { get; } =
        CharacterClassCatalog.AllClasses.Select(classDefinition => new OwnedCharacter(classDefinition)).ToList();

    public static OwnedCharacter Leader { get; } =
        Roster.First(character => character.ClassDefinition == CharacterClassCatalog.Krieger);

    public static List<OwnedCharacter> SelectedCompanions { get; } = new();

    /// <summary>Nimmt einen neu erworbenen Charakter ins Roster auf (Issue #6, Charakter-Shop) - erlaubt auch Duplikate einer bereits besessenen Klasse. Zufällige Stat-Varianten je nach Pack-Stufe sind bewusst noch nicht Teil davon, siehe Issue #29.</summary>
    public static OwnedCharacter AddCharacter(CharacterClassDefinition classDefinition)
    {
        var character = new OwnedCharacter(classDefinition);
        Roster.Add(character);
        return character;
    }

    /// <summary>Wird ein Gruppenmitglied besiegt (Companion.TakeDamage, Issue #7): dauerhaft aus dem Roster entfernt (bleibt aber als Datensatz erhalten, nur nicht mehr wählbar/lebend) und aus der aktuellen Gruppenauswahl geworfen. Betrifft nie den Leader - dessen Niederlage beendet stattdessen sofort den Run (siehe Arena._Process).</summary>
    public static void MarkDead(OwnedCharacter character)
    {
        character.IsAlive = false;
        SelectedCompanions.Remove(character);
    }
}
