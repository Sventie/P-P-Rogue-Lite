namespace PPRogueLite.Cards;

using System.Collections.Generic;

public static class CardCatalog
{
    public static List<CardDefinition> BuildWarriorStartingDeck()
    {
        var deck = new List<CardDefinition>();

        for (int i = 0; i < 4; i++)
        {
            deck.Add(new HiebCard());
        }

        for (int i = 0; i < 2; i++)
        {
            deck.Add(new WuchtschlagCard());
        }

        for (int i = 0; i < 2; i++)
        {
            deck.Add(new ParadeCard());
        }

        deck.Add(new FinteCard());
        deck.Add(new AtemHolenCard());

        return deck;
    }

    /// <summary>
    /// Karten, die der Krieger zusätzlich zum Startdeck besitzt, aber
    /// aktuell nicht im Kampf-Deck hat - für die "nicht im Deck"-Spalte im
    /// Deck-Screen.
    /// </summary>
    public static List<CardDefinition> BuildWarriorBenchCards()
    {
        var bench = new List<CardDefinition>();

        for (int i = 0; i < 2; i++)
        {
            bench.Add(new HiebCard());
        }

        for (int i = 0; i < 2; i++)
        {
            bench.Add(new WuchtschlagCard());
        }

        bench.Add(new ParadeCard());
        bench.Add(new FinteCard());
        bench.Add(new AtemHolenCard());

        return bench;
    }

    /// <summary>Ein Exemplar jedes bekannten Kartentyps - für Übersichten, die alle Typen auflisten sollen (auch mit 0 Stück).</summary>
    public static IReadOnlyList<CardDefinition> AllCardTypes() => new List<CardDefinition>
    {
        new HiebCard(),
        new WuchtschlagCard(),
        new ParadeCard(),
        new FinteCard(),
        new AtemHolenCard(),
    };
}
