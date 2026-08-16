namespace PPRogueLite.Character;

/// <summary>Krieger: ausgewogener Nahkämpfer, bisher der einzige spielbare Charakter (Rurik Steinfaust) - jetzt als Katalogeintrag formalisiert, Player.cs baut seinen PlayerCharacter weiterhin fest daraus.</summary>
public sealed class KriegerDefinition : CharacterClassDefinition
{
    public override string Name => "Rurik Steinfaust";

    public override string ClassName => "Krieger";

    public override int Strength => 16;

    public override int Dexterity => 12;

    public override int Constitution => 14;

    public override int Intelligence => 10;

    public override int Wisdom => 10;

    public override int Charisma => 8;

    public override int MaxHp => 24;

    public override int BaseArmorClass => 15;

    public override string StartingCardId => "hieb";
}
