namespace PPRogueLite.Combat;

using System;

public static class Dice
{
    private static readonly Random Rng = new();

    public static int Roll(int sides) => Rng.Next(1, sides + 1);
}
