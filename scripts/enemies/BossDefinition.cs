namespace PPRogueLite.Enemies;

/// <summary>
/// Erweiterte EnemyDefinition für Bossgegner (Issue #11): zusätzlich zu den
/// normalen Kampfwerten zwei Spezialfähigkeiten mit eigenem Cooldown -
/// periodisches Herbeirufen von Verstärkung (SummonedEnemy) und ein
/// telegraphierter Nahangriff nach vorne (Keulenschlag beim
/// Oger-Häuptling: Hitbox wird vor dem Treffer sichtbar, damit der Spieler
/// durch Wegbewegen ausweichen kann).
///
/// Movement ist fest auf EnemyMovement.Boss verdrahtet - der gemeinsame
/// Enemy-Node behandelt Bosse über eine eigene UpdateBoss-Methode statt der
/// Melee/Ranged/HitAndRun-Fälle, siehe Enemy.cs.
///
/// Bewusst konkret auf den Oger-Häuptling zugeschnitten (keine spekulative
/// Generalisierung) - künftige Bosse (Junger Drache, Kobold-Hexenmeister,
/// siehe CLAUDE.md) brauchen eigene Spezialmechaniken (Feueratem-Telegraph,
/// Teleport-Ausweichen) und werden diese Basisklasse bei Bedarf erweitern,
/// nicht heute schon vorwegnehmen.
/// </summary>
public abstract class BossDefinition : EnemyDefinition
{
    public override EnemyMovement Movement => EnemyMovement.Boss;

    /// <summary>Sekunden zwischen zwei Verstärkungs-Rufen.</summary>
    public abstract float SummonCooldown { get; }

    /// <summary>Gegnertyp, der pro Ruf gespawnt wird.</summary>
    public abstract EnemyDefinition SummonedEnemy { get; }

    /// <summary>Anzahl der Gegner pro Ruf.</summary>
    public abstract int SummonCount { get; }

    /// <summary>Sekunden zwischen zwei Keulenschlägen.</summary>
    public abstract float SlamCooldown { get; }

    /// <summary>Wie lange die Hitbox als Warnung sichtbar ist, bevor der Schlag auflöst.</summary>
    public abstract float SlamTelegraphDuration { get; }

    /// <summary>Reichweite des Keulenschlags nach vorne, ausgehend von der Position des Bosses.</summary>
    public abstract float SlamRange { get; }

    /// <summary>Halbe Breite der rechteckigen Keulenschlag-Hitbox.</summary>
    public abstract float SlamHalfWidth { get; }

    public abstract int SlamAttackBonus { get; }

    public abstract int SlamDamageDie { get; }

    public abstract int SlamDamageBonus { get; }
}
