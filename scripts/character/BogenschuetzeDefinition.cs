namespace PPRogueLite.Character;

/// <summary>Bogenschütze: GES-fokussierter Fernkämpfer, weniger HP/RK als der Krieger, dafür Reichweite über die eigene Pfeilschuss-Startkarte (siehe scripts/cards/PfeilschussCard.cs). Noch nicht auswählbar, siehe CharacterClassDefinition.</summary>
public sealed class BogenschuetzeDefinition : CharacterClassDefinition
{
    public override string Name => "Elara Windläufer";

    public override string ClassName => "Bogenschütze";

    public override int Strength => 10;

    public override int Dexterity => 18;

    public override int Constitution => 12;

    public override int Intelligence => 10;

    public override int Wisdom => 12;

    public override int Charisma => 8;

    public override int MaxHp => 18;

    public override int BaseArmorClass => 13;

    public override string StartingCardId => "pfeilschuss";
}
