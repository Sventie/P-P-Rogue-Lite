namespace PPRogueLite.Enemies;

/// <summary>Höhlentroll: Tank - viel HP/RK, langsam, geringer Schaden pro Treffer.</summary>
public sealed class TrollDefinition : EnemyDefinition
{
    public override string Id => "troll";

    public override string DisplayName => "Höhlentroll";

    public override EnemyMovement Movement => EnemyMovement.Melee;

    public override EnemyOnHitEffect OnHitEffect => EnemyOnHitEffect.None;

    public override int MaxHp => 30;

    public override int ArmorClass => 10;

    public override int AttackBonus => 3;

    public override int DamageDie => 8;

    public override int DamageBonus => 1;

    public override float MoveSpeed => 40f;

    public override float EngagementRange => 34f;

    public override float AttackCooldown => 1.8f;

    public override float Radius => 22f;

    public override int MinStage => 3;
}
