namespace PPRogueLite.Enemies;

/// <summary>Höhlengoblin: Standard-Nahkämpfer, ab Stage 1 verfügbar (ursprünglicher Gegner aus Issue #2).</summary>
public sealed class GoblinDefinition : EnemyDefinition
{
    public override string Id => "goblin";

    public override string DisplayName => "Höhlengoblin";

    public override EnemyMovement Movement => EnemyMovement.Melee;

    public override EnemyOnHitEffect OnHitEffect => EnemyOnHitEffect.None;

    public override int MaxHp => 12;

    public override int ArmorClass => 8;

    public override int AttackBonus => 4;

    public override int DamageDie => 6;

    public override int DamageBonus => 2;

    public override float MoveSpeed => 80f;

    public override float EngagementRange => 26f;

    public override float AttackCooldown => 1.0f;

    public override float Radius => 14f;

    public override int MinStage => 1;
}
