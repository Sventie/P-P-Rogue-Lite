namespace PPRogueLite.Character;

using System.Collections.Generic;

/// <summary>
/// Ein Charakter-Archetyp (Issue #13): Basiswerte + Name/Klasse + Start-
/// Aktionskarte, aus denen sich eine PlayerCharacter-Instanz bauen lässt.
/// Aktuell nur als Katalog vorbereitet - Player.cs baut weiterhin fest den
/// Krieger (jetzt aus KriegerDefinition statt inline-Werten), echte
/// Auswahl vor einem Dungeon-Lauf ist bewusst noch nicht verdrahtet, siehe
/// Issue #26 (braucht die Gruppe aus #5). Bogenschütze und Magier haben
/// eine eigene Fernkampf-Startkarte (PfeilschussCard/ArkanerBlitzCard) statt
/// des Krieger-Hiebs - echte, spielbare Mechaniken, schon jetzt über den
/// Deck-Screen mit dem Krieger testbar. Tank teilt sich Hieb mit dem
/// Krieger - seine Identität kommt bewusst rein aus den Stats (hohe
/// HP/RK), keine eigene Angriffsmechanik nötig.
/// </summary>
public abstract class CharacterClassDefinition
{
    public abstract string Name { get; }

    public abstract string ClassName { get; }

    public abstract int Strength { get; }

    public abstract int Dexterity { get; }

    public abstract int Constitution { get; }

    public abstract int Intelligence { get; }

    public abstract int Wisdom { get; }

    public abstract int Charisma { get; }

    public abstract int MaxHp { get; }

    public abstract int BaseArmorClass { get; }

    /// <summary>Id der Aktionskarte, mit der ein Run garantiert startet (analog zum bisherigen "immer mit Hieb starten", siehe Player._Ready).</summary>
    public abstract string StartingCardId { get; }

    public AbilityScores BuildStats() => new(new Dictionary<Ability, int>
    {
        [Ability.Strength] = Strength,
        [Ability.Dexterity] = Dexterity,
        [Ability.Constitution] = Constitution,
        [Ability.Intelligence] = Intelligence,
        [Ability.Wisdom] = Wisdom,
        [Ability.Charisma] = Charisma,
    });
}
