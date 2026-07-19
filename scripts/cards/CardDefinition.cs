namespace PPRogueLite.Cards;

using PPRogueLite.Combat;

public abstract class CardDefinition
{
    public abstract string Id { get; }

    public abstract string DisplayName { get; }

    public abstract string CardType { get; }

    public abstract string Description { get; }

    public abstract string RequirementText { get; }

    public virtual bool IsExhaust => false;

    public abstract void Play(CombatEngine engine);
}
