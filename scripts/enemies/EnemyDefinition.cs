namespace PPRogueLite.Enemies;

/// <summary>Bewegungs-/Engagement-Muster eines Gegnertyps (Issue #9).</summary>
public enum EnemyMovement
{
    /// <summary>Läuft direkt auf den Spieler zu, Kontaktangriff.</summary>
    Melee,

    /// <summary>Hält Abstand (EngagementRange), feuert Projektile.</summary>
    Ranged,

    /// <summary>Nähert sich, greift bei Kontakt an, zieht sich danach kurz zurück.</summary>
    HitAndRun,
}

/// <summary>
/// Zusatzeffekt bei einem Treffer, unabhängig vom Bewegungsmuster (z. B.
/// ein Fernkämpfer UND ein Nahkämpfer könnten theoretisch beide Slow
/// anwenden) - bewusst als eigene Achse statt in EnemyMovement verwoben.
/// </summary>
public enum EnemyOnHitEffect
{
    None,
    Slow,
}

/// <summary>
/// Beschreibt einen Gegnertyp (Issue #9): Stats + Bewegungsmuster +
/// Treffer-Effekt. Reine Datenklasse, kein Godot-Bezug (auch keine
/// Color/Vector2 - Visuals leben in Enemy.cs). Der gemeinsame Enemy-Node
/// verzweigt sein Verhalten über Movement/OnHitEffect statt dass jeder
/// Gegnertyp ein eigenes Godot-Script bräuchte - vermeidet, dass
/// Distanzberechnung/Zeichnen/HP-Balken pro Typ dupliziert werden.
/// </summary>
public abstract class EnemyDefinition
{
    public abstract string Id { get; }

    public abstract string DisplayName { get; }

    public abstract EnemyMovement Movement { get; }

    public abstract EnemyOnHitEffect OnHitEffect { get; }

    public abstract int MaxHp { get; }

    public abstract int ArmorClass { get; }

    public abstract int AttackBonus { get; }

    public abstract int DamageDie { get; }

    public abstract int DamageBonus { get; }

    public abstract float MoveSpeed { get; }

    /// <summary>Kontaktreichweite (Melee/HitAndRun) bzw. Wunschabstand zum Spieler (Ranged).</summary>
    public abstract float EngagementRange { get; }

    public abstract float AttackCooldown { get; }

    public abstract float Radius { get; }

    /// <summary>Ab welcher Dungeon-Stage dieser Typ im Spawn-Pool auftaucht (gestaffelte Einführung, Issue #9).</summary>
    public abstract int MinStage { get; }
}
