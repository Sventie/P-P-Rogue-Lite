namespace PPRogueLite.Enemies;

/// <summary>Kobold-Schamane: Fernkämpfer wie der Bogenschütze, aber mit Verlangsamungs-Effekt statt reinem Schaden-Fokus.</summary>
public sealed class ShamanDefinition : EnemyDefinition
{
    public override string Id => "schamane";

    public override string DisplayName => "Kobold-Schamane";

    public override EnemyMovement Movement => EnemyMovement.Ranged;

    public override EnemyOnHitEffect OnHitEffect => EnemyOnHitEffect.Slow;

    public override int MaxHp => 10;

    public override int ArmorClass => 9;

    public override int AttackBonus => 3;

    public override int DamageDie => 4;

    public override int DamageBonus => 1;

    public override float MoveSpeed => 60f;

    public override float EngagementRange => 300f;

    public override float AttackCooldown => 2.5f;

    public override float Radius => 13f;

    public override int MinStage => 4;
}
