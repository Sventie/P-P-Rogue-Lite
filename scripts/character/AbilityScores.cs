namespace PPRogueLite.Character;

using System;
using System.Collections.Generic;

public enum Ability
{
    Strength,
    Dexterity,
    Constitution,
    Intelligence,
    Wisdom,
    Charisma,
}

public sealed class AbilityScores
{
    private readonly Dictionary<Ability, int> _scores;

    public AbilityScores(Dictionary<Ability, int> scores)
    {
        _scores = scores;
    }

    public int Score(Ability ability) => _scores[ability];

    public int Modifier(Ability ability) => (int)Math.Floor((_scores[ability] - 10) / 2.0);
}
