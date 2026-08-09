namespace PPRogueLite.Enemies;

/// <summary>
/// Boss der Stage 10 (Issue #11): tankiger Nahkämpfer, der die goblinische
/// Gegner-Vielfalt aus Issue #9 anführt. Zwei Spezialfähigkeiten oben auf
/// dem normalen Kontaktangriff (siehe BossDefinition): ruft periodisch
/// Höhlengoblin-Verstärkung, und schlägt in größeren Abständen mit einem
/// schweren, telegraphierten Keulenschlag nach vorne zu (Hitbox wird
/// SlamTelegraphDuration lang sichtbar, bevor der Schlag auflöst - der
/// Spieler kann durch Wegbewegen ausweichen).
/// </summary>
public sealed class OgerHaeuptlingDefinition : BossDefinition
{
    public override string Id => "oger_haeuptling";

    public override string DisplayName => "Oger-Häuptling";

    public override EnemyOnHitEffect OnHitEffect => EnemyOnHitEffect.None;

    public override int MaxHp => 140;

    public override int ArmorClass => 13;

    public override int AttackBonus => 6;

    public override int DamageDie => 8;

    public override int DamageBonus => 4;

    public override float MoveSpeed => 55f;

    public override float EngagementRange => 50f;

    public override float AttackCooldown => 1.4f;

    public override float Radius => 34f;

    public override int MinStage => 10;

    public override float SummonCooldown => 12f;

    public override EnemyDefinition SummonedEnemy => new GoblinDefinition();

    public override int SummonCount => 2;

    public override float SlamCooldown => 7f;

    public override float SlamTelegraphDuration => 1.0f;

    public override float SlamRange => 220f;

    public override float SlamHalfWidth => 55f;

    public override int SlamAttackBonus => 7;

    public override int SlamDamageDie => 12;

    public override int SlamDamageBonus => 8;
}
