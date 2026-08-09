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

public abstract class CardDefinition
{
    public abstract string Id { get; }

    public abstract string DisplayName { get; }

    public abstract string CardType { get; }

    public abstract string Description { get; }

    public abstract string RequirementText { get; }

    public virtual bool IsExhaust => false;

    public virtual CardKind Kind => CardKind.Action;

    /// <summary>
    /// Nur für gekoppelte Modifikatorkarten: die Id der Aktionskarte, an die
    /// sich dieser Modifikator hängt (null für alle anderen Karten).
    /// </summary>
    public virtual string? CoupledCardId => null;

    public abstract void Play(CombatEngine engine);
}
