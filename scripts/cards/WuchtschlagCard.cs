namespace PPRogueLite.Cards;

using PPRogueLite.Character;
using PPRogueLite.Combat;

public sealed class WuchtschlagCard : CardDefinition
{
    public override string Id => "wuchtschlag";

    public override string DisplayName => "Wuchtschlag";

    public override string CardType => "Angriff";

    public override CardRarity Rarity => CardRarity.Bronze;

    public override string Description => "Rücksichtsloser Schlag mit voller Kraft — schwerer zu treffen, aber verheerend.";

    public override string RequirementText => "W20 + STR − 2, dafür 2W8 Schaden";

    public override void Play(CombatEngine engine)
    {
        int fullModifier = engine.Player.Stats.Modifier(Ability.Strength);
        int attackModifier = fullModifier - 2;
        var result = engine.AttackRoll(attackModifier, $"Wuchtschlag (STR {CombatEngine.FormatMod(attackModifier)})");
        if (!result.IsHit)
        {
            return;
        }

        int firstDie = Dice.Roll(8);
        int secondDie = Dice.Roll(8);
        int damage = firstDie + secondDie + fullModifier;
        engine.Foe.TakeDamage(damage);
        engine.Log($"Voller Wucht! Schaden: {firstDie}+{secondDie} (2W8) {CombatEngine.FormatMod(fullModifier)} = [b]{damage}[/b]", LogTag.Good);
    }
}
