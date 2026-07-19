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
}
