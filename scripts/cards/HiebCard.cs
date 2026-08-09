namespace PPRogueLite.Cards;

using PPRogueLite.Character;
using PPRogueLite.Combat;

public sealed class HiebCard : CardDefinition
{
    public override string Id => "hieb";

    public override string DisplayName => "Hieb";

    public override string CardType => "Angriff";

    public override CardRarity Rarity => CardRarity.Bronze;

    public override string Description => "Ein gezielter Schwerthieb.";

    public override string RequirementText => "W20 + STR gegen RK";

    public override void Play(CombatEngine engine)
    {
        int modifier = engine.Player.Stats.Modifier(Ability.Strength);
        var result = engine.AttackRoll(modifier, $"Hieb (STR {CombatEngine.FormatMod(modifier)})");
        if (!result.IsHit)
        {
            return;
        }

        int damageRoll = Dice.Roll(8);
        int damage = damageRoll + modifier;
        engine.Foe.TakeDamage(damage);
        engine.Log($"Treffer! Schaden: {damageRoll} (W8) {CombatEngine.FormatMod(modifier)} = [b]{damage}[/b]", LogTag.Good);
    }
}
