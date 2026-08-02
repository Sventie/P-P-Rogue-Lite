namespace PPRogueLite.Character;

using System;

public sealed class Enemy
{
    public string Name { get; init; } = string.Empty;

    public int MaxHp { get; init; }

    public int Hp { get; set; }

    public int ArmorClass { get; init; }

    public int AttackBonus { get; init; }

    public int DamageDie { get; init; }

    public int DamageBonus { get; init; }

    public bool IsDefeated => Hp <= 0;

    public void TakeDamage(int amount) => Hp = Math.Max(0, Hp - amount);
}
