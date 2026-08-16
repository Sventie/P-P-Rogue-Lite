namespace PPRogueLite.Character;

using System.Collections.Generic;

/// <summary>Alle bekannten Charakter-Archetypen (Issue #13). Aktuell nur Datengrundlage - Player.cs nutzt fest Krieger, echte Auswahl folgt mit Issue #26/#5.</summary>
public static class CharacterClassCatalog
{
    public static CharacterClassDefinition Krieger { get; } = new KriegerDefinition();

    public static IReadOnlyList<CharacterClassDefinition> AllClasses { get; } = new List<CharacterClassDefinition>
    {
        Krieger,
        new BogenschuetzeDefinition(),
        new MagierDefinition(),
        new TankDefinition(),
    };
}
