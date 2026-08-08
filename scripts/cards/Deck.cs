namespace PPRogueLite.Cards;

using System;
using System.Collections.Generic;
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
    public Deck(IEnumerable<CardDefinition> startingCards, bool shuffleOnCreate = true)
    {
        _drawPile.AddRange(startingCards);
        if (shuffleOnCreate)
        {
            Shuffle(_drawPile);
        }
    }

    public IReadOnlyList<CardDefinition> DrawPile => _drawPile;

    public IReadOnlyList<CardDefinition> DiscardPile => _discardPile;

    public List<CardDefinition> DrawHand(int size, Action<string, LogTag>? log = null)
    {
        var hand = new List<CardDefinition>();

        while (hand.Count < size)
        {
            if (_drawPile.Count == 0)
            {
                if (_discardPile.Count == 0)
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

    private void Shuffle(List<CardDefinition> cards)
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }
}
