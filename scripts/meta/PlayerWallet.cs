namespace PPRogueLite.Meta;

/// <summary>
/// Gold-Bestand des Spielers, prozessweit über Szenenwechsel hinweg (wie
/// PlayerCardCollection - statisches Feld statt Autoload). Aktuell nur als
/// Stage-Belohnung befüllt, noch nirgends ausgegeben (Card Shop folgt in
/// einem späteren Issue).
/// </summary>
public static class PlayerWallet
{
    public static int Gold { get; set; }
}
