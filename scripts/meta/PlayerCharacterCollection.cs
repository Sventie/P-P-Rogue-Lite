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
/// (Issue #5 - der Character-Shop aus Issue #6 existiert noch nicht, ein
/// leerer Besitz hätte die Gruppen-Auswahl also ins Leere laufen lassen).
/// Leader ist fest der Krieger (Hauptcharakter, weiterhin von Player.cs
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

    /// <summary>Wird ein Gruppenmitglied besiegt (Companion.TakeDamage, Issue #7): dauerhaft aus dem Roster entfernt (bleibt aber als Datensatz erhalten, nur nicht mehr wählbar/lebend) und aus der aktuellen Gruppenauswahl geworfen. Betrifft nie den Leader - dessen Niederlage beendet stattdessen sofort den Run (siehe Arena._Process).</summary>
    public static void MarkDead(OwnedCharacter character)
    {
        character.IsAlive = false;
        SelectedCompanions.Remove(character);
    }
}
