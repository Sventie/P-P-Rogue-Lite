namespace PPRogueLite.Meta;

using System;
using System.Collections.Generic;
using System.Linq;
using PPRogueLite.Cards;

/// <summary>
/// Zustand des Shops, prozessweit über Szenenwechsel hinweg (gleiches
/// static-Muster wie PlayerWallet/PlayerCardCollection, Issue #4). Die 3
/// Sonderangebote werden einmalig gewürfelt, sobald der Shop zum ersten
/// Mal betreten wird (EnsureSpecialOffersRolled), und erst wieder neu
/// gewürfelt, wenn ein Dungeon endet (DungeonRun.End() ruft
/// RerollSpecialOffers auf) - der Shop ist ohnehin nur über die Taverne
/// erreichbar, nicht über das Lager zwischen zwei Stages eines laufenden
/// Dungeons, die Angebote bleiben also für die Dauer eines Dungeons fest.
/// </summary>
public static class ShopState
{
    private const int SpecialOfferCount = 3;

    private static readonly Random Rng = new();
    private static bool _hasRolled;

    public static List<CardDefinition> SpecialOffers { get; private set; } = new();

    /// <summary>
    /// Würfelt die Sonderangebote nur beim allerersten Aufruf - danach
    /// bleiben sie unangetastet, auch wenn der Spieler alle 3 gekauft hat
    /// (SpecialOffers dann leer), bis der nächste Dungeon endet.
    /// </summary>
    public static void EnsureSpecialOffersRolled()
    {
        if (_hasRolled)
        {
            return;
        }

        RerollSpecialOffers();
    }

    public static void RerollSpecialOffers()
    {
        _hasRolled = true;
        SpecialOffers = CardCatalog.AllCardTypes()
            .OrderBy(_ => Rng.Next())
            .Take(SpecialOfferCount)
            .ToList();
    }
}
