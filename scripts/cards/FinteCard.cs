namespace PPRogueLite.Cards;

using PPRogueLite.Character;
using PPRogueLite.Combat;

public sealed class FinteCard : CardDefinition
{
    public override string Id => "finte";

    public override string DisplayName => "Finte";

    public override string CardType => "Manöver";

    public override string Description => "Du täuschst den Gegner. Bei Erfolg hat deine nächste Angriffskarte Vorteil (zweimal würfeln, besseres Ergebnis).";

    public override string RequirementText => "W20 + GES gegen 10";

    public override void Play(CombatEngine engine)
    {
        int modifier = engine.Player.Stats.Modifier(Ability.Dexterity);
        var result = engine.AbilityCheck(modifier, 10, "Finte");
        if (result.Success)
        {
            engine.Player.HasAdvantageNextAttack = true;
            engine.Log("Der Goblin tappt in die Falle — dein nächster Angriff hat [b]Vorteil[/b].", LogTag.Good);
        }
        else
        {
            engine.Log("Der Goblin lässt sich nicht täuschen.", LogTag.Bad);
        }
    }
}
