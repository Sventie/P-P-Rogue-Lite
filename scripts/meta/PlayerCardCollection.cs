namespace PPRogueLite.Meta;

using System.Collections.Generic;
using PPRogueLite.Cards;

/// <summary>
/// Hält den kompletten Kartenbesitz über Szenenwechsel hinweg (statische
/// Felder überleben, solange der Prozess läuft): welche Karten aktuell im
/// Kampf-Deck sind und welche im Bestand, aber nicht im Deck.
///
/// Player.cs kopiert DeckCards beim Arena-Start in ein laufeigenes Deck -
/// Änderungen im Deck-Screen wirken sich also auf den nächsten Run aus,
/// nie auf einen laufenden.
/// </summary>
public static class PlayerCardCollection
{
    public static List<CardDefinition> DeckCards { get; } = CardCatalog.BuildWarriorStartingDeck();

    public static List<CardDefinition> BenchCards { get; } = CardCatalog.BuildWarriorBenchCards();
}
