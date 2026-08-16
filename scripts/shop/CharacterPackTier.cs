namespace PPRogueLite.ShopSystem;

using PPRogueLite.Cards;

/// <summary>
/// Eine Charakter-Pack-Stufe (Issue #6): anders als CardPackTier hat die
/// Stufe aktuell KEINEN Effekt auf den gezogenen Charakter selbst - nur
/// Anzeigename und Preis steigen mit der Stufe, der gezogene Archetyp ist
/// unabhängig von der Stufe gleichverteilt aus CharacterClassCatalog.AllClasses
/// gewählt (siehe CharacterPackOpener). Zufällige Stat-Varianten je nach
/// Pack-Stufe sind als eigenes Issue vorgemerkt (siehe CLAUDE.md, Issue #29) -
/// Rarity dient hier bewusst nur der Namensgebung/Preisstaffelung, analog zu
/// CardPackTier.Rarity.
/// </summary>
public sealed record CharacterPackTier(CardRarity Rarity, string DisplayName, int Price);
