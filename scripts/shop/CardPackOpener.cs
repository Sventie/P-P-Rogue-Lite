namespace PPRogueLite.ShopSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using PPRogueLite.Cards;

/// <summary>
/// Öffnet ein Kartenpack (Issue #4): würfelt PackCardCount Karten, jede
/// unabhängig von den anderen - zuerst die Rarität nach der Gewichtung der
/// Pack-Stufe, dann eine gleichverteilt zufällige Karte dieser Rarität aus
/// CardCatalog.AllCardTypes().
/// </summary>
public static class CardPackOpener
{
    public const int PackCardCount = 3;

    private static readonly Random Rng = new();

    public static List<CardDefinition> Open(CardPackTier tier)
    {
        var result = new List<CardDefinition>();
        for (int i = 0; i < PackCardCount; i++)
        {
            var rarity = RollRarity(tier);
            result.Add(PickRandomCardOfRarity(rarity));
        }

        return result;
    }

    private static CardRarity RollRarity(CardPackTier tier)
    {
        int roll = Rng.Next(1, 101);
        int cumulative = 0;
        foreach (var (rarity, weight) in tier.RarityWeights)
        {
            cumulative += weight;
            if (roll <= cumulative)
            {
                return rarity;
            }
        }

        // Falls die Gewichte sich nicht exakt zu 100 summieren: die
        // namensgebende Rarität der Pack-Stufe hat garantiert Kandidaten.
        return tier.Rarity;
    }

    private static CardDefinition PickRandomCardOfRarity(CardRarity rarity)
    {
        var candidates = CardCatalog.AllCardTypes().Where(card => card.Rarity == rarity).ToList();
        return candidates[Rng.Next(candidates.Count)];
    }
}
