namespace PPRogueLite.Combat;

public readonly struct AttackResult
{
    public int Roll { get; init; }

    public int Total { get; init; }

    public bool IsHit { get; init; }
}
