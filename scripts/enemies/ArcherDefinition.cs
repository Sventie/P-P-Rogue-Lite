namespace PPRogueLite.Enemies;

/// <summary>Goblin-Bogenschütze: Fernkämpfer, hält Abstand und feuert Pfeile (Projektile).</summary>
public sealed class ArcherDefinition : EnemyDefinition
{
    public override string Id => "archer";

    public override string DisplayName => "Goblin-Bogenschütze";

    public override EnemyMovement Movement => EnemyMovement.Ranged;

    public override EnemyOnHitEffect OnHitEffect => EnemyOnHitEffect.None;

    public override int MaxHp => 8;

    public override int ArmorClass => 8;

    public override int AttackBonus => 3;

    public override int DamageDie => 6;

    public override int DamageBonus => 1;

    public override float MoveSpeed => 70f;

    public override float EngagementRange => 260f;

    public override float AttackCooldown => 1.6f;

    public override float Radius => 12f;

    public override int MinStage => 2;
}
