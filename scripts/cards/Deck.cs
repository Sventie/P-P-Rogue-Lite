namespace PPRogueLite.Cards;

using System;
using System.Collections.Generic;
using System.Linq;
using PPRogueLite.Combat;

/// <summary>
/// Nachziehstapel/Hand/Ablage-Verwaltung. Kein Godot-Bezug, damit sich die
/// Logik später leicht wiederverwenden/testen lässt.
/// </summary>
public sealed class Deck
{
    private readonly List<CardDefinition> _drawPile = new();
    private readonly List<CardDefinition> _discardPile = new();
    private readonly Random _rng = new();

    /// <summary>
    /// shuffleOnCreate=false wird beim Fortsetzen eines Dungeons gebraucht:
    /// der gespeicherte Nachziehstapel hat schon eine feste (gemischte)
    /// Reihenfolge, die beim Wiederherstellen nicht erneut gemischt werden
    /// darf (siehe DungeonRun/Player.SaveProgress).
    /// </summary>
    public Deck(IEnumerable<CardDefinition> startingCards, bool shuffleOnCreate = true, IEnumerable<CardDefinition>? discardedCards = null)
    {
        _drawPile.AddRange(startingCards);
        if (discardedCards is not null)
        {
            _discardPile.AddRange(discardedCards);
        }

        if (shuffleOnCreate)
        {
            Shuffle(_drawPile);
        }
    }

    public IReadOnlyList<CardDefinition> DrawPile => _drawPile;

    public IReadOnlyList<CardDefinition> DiscardPile => _discardPile;

    /// <summary>
    /// allowReshuffleFromDiscard steuert, ob ein leerer Nachziehstapel
    /// automatisch aus der Ablage neu gemischt wird. Der alte rundenbasierte
    /// Kampf (Main.cs/CombatEngine) braucht das pro Zug; die Echtzeit-Arena
    /// (Issue #15) übergibt hier bewusst false - Karten auf der Ablage
    /// sollen für den Rest des Runs unerreichbar bleiben, bis eine künftige
    /// Karte den Stapel gezielt zurückmischt (siehe Issue-Backlog/CLAUDE.md).
    /// </summary>
    public List<CardDefinition> DrawHand(int size, Action<string, LogTag>? log = null, bool allowReshuffleFromDiscard = true)
    {
        var hand = new List<CardDefinition>();

        while (hand.Count < size)
        {
            if (_drawPile.Count == 0)
            {
                if (!allowReshuffleFromDiscard || _discardPile.Count == 0)
                {
                    break;
                }

                _drawPile.AddRange(_discardPile);
                _discardPile.Clear();
                Shuffle(_drawPile);
                log?.Invoke("Der Ablagestapel wird neu gemischt.", LogTag.System);
            }

            int lastIndex = _drawPile.Count - 1;
            hand.Add(_drawPile[lastIndex]);
            _drawPile.RemoveAt(lastIndex);
        }

        return hand;
    }

    /// <summary>
    /// Legt die Hand ab; die gespielte Karte (playedIndex) wird bei
    /// Exhaust-Karten entfernt statt abgelegt.
    /// </summary>
    public void ResolveHand(IReadOnlyList<CardDefinition> hand, int playedIndex)
    {
        for (int i = 0; i < hand.Count; i++)
        {
            if (i == playedIndex && hand[i].IsExhaust)
            {
                continue;
            }

            _discardPile.Add(hand[i]);
        }
    }

    /// <summary>Legt eine einzelne Karte auf die Ablage (Issue #15: nicht gewählte Karte bei der Level-up-Auswahl).</summary>
    public void Discard(CardDefinition card)
    {
        _discardPile.Add(card);
    }

    /// <summary>Mischt die komplette Ablage zurück in den Nachziehstapel (Issue #21, ausgelöst über die Wiederkehr-Karte) - gleiche Mischlogik wie der automatische Reshuffle in DrawHand, hier aber gezielt auslösbar statt nur bei leerem Nachziehstapel.</summary>
    public void ReshuffleDiscardIntoDrawPile()
    {
        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        Shuffle(_drawPile);
    }

    /// <summary>Entfernt eine Karteninstanz mit der gegebenen Id aus der Ablage und gibt sie zurück (Issue #22: gezielte Rückholung einer Karte aus der Ablage über die Erinnerung-Karte). null, falls keine passende Karte mehr in der Ablage liegt.</summary>
    public CardDefinition? TakeFromDiscard(string cardId)
    {
        var match = _discardPile.FirstOrDefault(card => card.Id == cardId);
        if (match is not null)
        {
            _discardPile.Remove(match);
        }

        return match;
    }

    private void Shuffle(List<CardDefinition> cards)
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }
}
