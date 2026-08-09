namespace PPRogueLite.Cards;

using PPRogueLite.Combat;

/// <summary>
/// Aktionskarte (eigener Cooldown-Slot, wie bisher) vs. Modifikatorkarte
/// (passiv, kein Cooldown - wird einmalig beim Ausrüsten registriert statt
/// wiederkehrend auszulösen). Siehe Kartenplanung/Issue #12 in CLAUDE.md.
/// </summary>
public enum CardKind
{
    Action,
    Modifier,
}

/// <summary>
/// Seltenheitsstufe einer Karte (Issue #4: Card Shop) - entspricht 1:1 den
/// Kartenpack-Stufen (PPRogueLite.Shop.CardPackCatalog), aus denen beim
/// Öffnen eines Packs gezogen wird. Bewusst abstrakt auf CardDefinition
/// (nicht virtual mit Default) - jede Karte muss explizit eine Stufe
/// bekommen, es gibt keinen sinnvollen impliziten Standardwert.
/// </summary>
public enum CardRarity
{
    Bronze,
    Silber,
    Gold,
    Platin,
    Diamant,
}

public abstract class CardDefinition
{
    public abstract string Id { get; }

    public abstract string DisplayName { get; }

    public abstract string CardType { get; }

    public abstract string Description { get; }

    public abstract string RequirementText { get; }

    public abstract CardRarity Rarity { get; }

    public virtual bool IsExhaust => false;

    public virtual CardKind Kind => CardKind.Action;

    /// <summary>
    /// Nur für gekoppelte Modifikatorkarten: die Id der Aktionskarte, an die
    /// sich dieser Modifikator hängt (null für alle anderen Karten).
    /// </summary>
    public virtual string? CoupledCardId => null;

    public abstract void Play(CombatEngine engine);
}
