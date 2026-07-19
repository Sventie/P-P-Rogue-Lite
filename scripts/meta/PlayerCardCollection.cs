namespace PPRogueLite.Meta;

using System.Collections.Generic;
using PPRogueLite.Cards;

/// <summary>
/// Hält den kompletten Kartenbesitz über Szenenwechsel hinweg (statische
/// Felder überleben, solange der Prozess läuft): welche Karten aktuell im
/// Kampf-Deck sind und welche im Bestand, aber nicht im Deck.
///
/// Aktuell nur für die Anzeige im Deck-Screen genutzt. Der Kampf baut sein
/// Deck weiterhin unabhängig über CardCatalog.BuildWarriorStartingDeck()
/// auf - eine Verbindung (Deck-Änderungen wirken sich auf den nächsten
/// Kampf aus) ist noch nicht hergestellt, siehe CLAUDE.md.
/// </summary>
public static class PlayerCardCollection
{
    public static List<CardDefinition> DeckCards { get; } = CardCatalog.BuildWarriorStartingDeck();

    public static List<CardDefinition> BenchCards { get; } = CardCatalog.BuildWarriorBenchCards();
}
