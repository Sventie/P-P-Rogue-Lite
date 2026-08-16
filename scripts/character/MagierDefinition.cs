namespace PPRogueLite.Character;

/// <summary>Magier: INT-fokussierter Fernkämpfer, am fragilsten (wenig HP/RK), dafür Reichweite über die eigene Arkaner-Blitz-Startkarte (siehe scripts/cards/ArkanerBlitzCard.cs). Noch nicht auswählbar, siehe CharacterClassDefinition.</summary>
public sealed class MagierDefinition : CharacterClassDefinition
{
    public override string Name => "Sylvara Nachtglanz";

    public override string ClassName => "Magier";

    public override int Strength => 8;

    public override int Dexterity => 12;

    public override int Constitution => 10;

    public override int Intelligence => 18;

    public override int Wisdom => 12;

    public override int Charisma => 10;

    public override int MaxHp => 14;

    public override int BaseArmorClass => 11;

    public override string StartingCardId => "arkaner_blitz";
}
