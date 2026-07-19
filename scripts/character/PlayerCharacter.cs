namespace PPRogueLite.Character;

using System;

public sealed class PlayerCharacter
{
    public string Name { get; init; } = string.Empty;

    public string ClassName { get; init; } = string.Empty;

    public AbilityScores Stats { get; init; } = null!;

    public int MaxHp { get; init; }

    public int Hp { get; set; }

    public int BaseArmorClass { get; init; }

    public int TempArmorClass { get; set; }

    public bool HasAdvantageNextAttack { get; set; }

    public int ArmorClass => BaseArmorClass + TempArmorClass;

    public bool IsDefeated => Hp <= 0;

    public void Heal(int amount) => Hp = Math.Min(MaxHp, Hp + amount);

    public void TakeDamage(int amount) => Hp = Math.Max(0, Hp - amount);
}
