namespace PPRogueLite.Enemies;

/// <summary>
/// Kobold-Schurke: schnell, Hit-and-Run (Kontaktangriff, danach kurzer
/// Rückzug statt Stehenbleiben). Macht bewusst nur normalen Schaden - ein
/// Gold-Diebstahl-Effekt ist als mögliche spätere Ergänzung/eigener
/// Gegnertyp gedacht, nicht Teil dieser ersten Umsetzung.
/// </summary>
public sealed class RogueDefinition : EnemyDefinition
{
    public override string Id => "schurke";

    public override string DisplayName => "Kobold-Schurke";

    public override EnemyMovement Movement => EnemyMovement.HitAndRun;

    public override EnemyOnHitEffect OnHitEffect => EnemyOnHitEffect.None;

    public override int MaxHp => 8;

    public override int ArmorClass => 11;

    public override int AttackBonus => 3;

    public override int DamageDie => 6;

    public override int DamageBonus => 0;

    public override float MoveSpeed => 110f;

    public override float EngagementRange => 24f;

    public override float AttackCooldown => 1.2f;

    public override float Radius => 12f;

    public override int MinStage => 3;
}
