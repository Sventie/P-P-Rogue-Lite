namespace PPRogueLite.ShopSystem;

using System.Collections.Generic;
using System.Linq;
using PPRogueLite.Cards;

/// <summary>
/// Die 5 Kartenpack-Stufen (Issue #4), eine pro Kartenrarität - Preis
/// steigt pro Stufe, ebenso die Wahrscheinlichkeit auf seltenere Karten.
/// Alle Zahlen sind Test-Balance-Werte (vom Nutzer als Beispiel
/// vorgegeben: Bronze 95 % Bronze/5 % Silber, Silber 20 % Bronze/75 %
/// Silber/5 % Gold) - werden im echten Balancing-Durchgang (Issue #17)
/// noch angepasst.
/// </summary>
public static class CardPackCatalog
{
    public static IReadOnlyList<CardPackTier> AllTiers { get; } = new List<CardPackTier>
    {
        new(CardRarity.Bronze, "Bronze-Paket", 1, new Dictionary<CardRarity, int>
        {
            [CardRarity.Bronze] = 95,
            [CardRarity.Silber] = 5,
        }),
        new(CardRarity.Silber, "Silber-Paket", 2, new Dictionary<CardRarity, int>
        {
            [CardRarity.Bronze] = 20,
            [CardRarity.Silber] = 75,
            [CardRarity.Gold] = 5,
        }),
        new(CardRarity.Gold, "Gold-Paket", 3, new Dictionary<CardRarity, int>
        {
            [CardRarity.Silber] = 20,
            [CardRarity.Gold] = 70,
            [CardRarity.Platin] = 10,
        }),
        new(CardRarity.Platin, "Platin-Paket", 4, new Dictionary<CardRarity, int>
        {
            [CardRarity.Gold] = 20,
            [CardRarity.Platin] = 70,
            [CardRarity.Diamant] = 10,
        }),
        new(CardRarity.Diamant, "Diamant-Paket", 5, new Dictionary<CardRarity, int>
        {
            [CardRarity.Platin] = 30,
            [CardRarity.Diamant] = 70,
        }),
    };

    /// <summary>Preis für den direkten Kauf einer Sonderangebot-Karte dieser Rarität (Issue #4) - Test-Balance-Wert, das Doppelte des Pack-Preises derselben Stufe.</summary>
    public static int SpecialOfferPriceFor(CardRarity rarity) =>
        AllTiers.First(tier => tier.Rarity == rarity).Price * 2;
}
