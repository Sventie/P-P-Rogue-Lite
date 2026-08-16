namespace PPRogueLite.Character;

/// <summary>Tank: KON-fokussierter Nahkämpfer, meiste HP/RK, dafür geringerer Schaden - Identität kommt bewusst rein aus den Stats (keine eigene Angriffsmechanik nötig), startet wie der Krieger mit Hieb. Noch nicht auswählbar, siehe CharacterClassDefinition.</summary>
public sealed class TankDefinition : CharacterClassDefinition
{
    public override string Name => "Brunhilde Eisenwall";

    public override string ClassName => "Tank";

    public override int Strength => 14;

    public override int Dexterity => 8;

    public override int Constitution => 18;

    public override int Intelligence => 8;

    public override int Wisdom => 10;

    public override int Charisma => 8;

    public override int MaxHp => 32;

    public override int BaseArmorClass => 17;

    public override string StartingCardId => "hieb";
}
