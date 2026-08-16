namespace PPRogueLite.ShopSystem;

using System.Collections.Generic;
using PPRogueLite.Cards;

/// <summary>
/// Die 5 Charakter-Pack-Stufen (Issue #6, gleiche Rarity-Namensgebung wie
/// CardPackCatalog) - Preis steigt pro Stufe, Test-Balance-Werte. Anders als
/// bei Kartenpacks hat die Stufe (noch) keinen Effekt auf den gezogenen
/// Charakter selbst, siehe CharacterPackTier.
/// </summary>
public static class CharacterPackCatalog
{
    public static IReadOnlyList<CharacterPackTier> AllTiers { get; } = new List<CharacterPackTier>
    {
        new(CardRarity.Bronze, "Bronze-Charakterpack", 15),
        new(CardRarity.Silber, "Silber-Charakterpack", 25),
        new(CardRarity.Gold, "Gold-Charakterpack", 40),
        new(CardRarity.Platin, "Platin-Charakterpack", 60),
        new(CardRarity.Diamant, "Diamant-Charakterpack", 90),
    };
}
