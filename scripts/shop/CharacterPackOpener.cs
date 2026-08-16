namespace PPRogueLite.Shop;

using System;
using PPRogueLite.Character;

/// <summary>
/// Öffnet ein Charakter-Pack (Issue #6): ein Pack enthält genau einen
/// Charakter, gleichverteilt zufällig aus allen bekannten Archetypen
/// (CharacterClassCatalog.AllClasses) - unabhängig von der Pack-Stufe, siehe
/// CharacterPackTier. Duplikate einer bereits besessenen Klasse sind
/// ausdrücklich erlaubt (Issue #6: "aktuell wird einfach ein weiterer
/// Bogenschütze erzeugt").
/// </summary>
public static class CharacterPackOpener
{
    private static readonly Random Rng = new();

    public static CharacterClassDefinition Open(CharacterPackTier tier)
    {
        var candidates = CharacterClassCatalog.AllClasses;
        return candidates[Rng.Next(candidates.Count)];
    }
}
