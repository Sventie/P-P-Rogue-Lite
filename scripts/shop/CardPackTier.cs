namespace PPRogueLite.ShopSystem;

using System.Collections.Generic;
using PPRogueLite.Cards;

/// <summary>
/// Eine Kartenpack-Stufe (Issue #4): Preis + Wahrscheinlichkeitsverteilung
/// (Prozentwerte, die sich zu 100 summieren sollten) über die
/// Kartenraritäten, aus der beim Öffnen gezogen wird. Rarity ist die
/// "namensgebende" Stufe des Packs (z. B. Bronze-Paket -> CardRarity.Bronze)
/// und wird auch für die Sonderangebot-Preisformel verwendet, siehe
/// CardPackCatalog.SpecialOfferPriceFor.
/// </summary>
public sealed class CardPackTier
{
    public CardPackTier(CardRarity rarity, string displayName, int price, IReadOnlyDictionary<CardRarity, int> rarityWeights)
    {
        Rarity = rarity;
        DisplayName = displayName;
        Price = price;
        RarityWeights = rarityWeights;
    }

    public CardRarity Rarity { get; }

    public string DisplayName { get; }

    public int Price { get; }

    public IReadOnlyDictionary<CardRarity, int> RarityWeights { get; }
}
