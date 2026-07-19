namespace PPRogueLite.Combat;

public readonly struct AbilityCheckResult
{
    public int Roll { get; init; }

    public int Total { get; init; }

    public bool Success { get; init; }
}
