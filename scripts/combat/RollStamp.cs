namespace PPRogueLite.Combat;

/// <summary>
/// Begleitet eine Log-Zeile, die aus einem Würfelwurf stammt, mit dem
/// Erfolg/Misserfolg-Signal fürs Popup (z. B. "TREFFER" / "VERFEHLT").
/// </summary>
public readonly record struct RollStamp(bool Success, string SuccessText, string FailureText);
