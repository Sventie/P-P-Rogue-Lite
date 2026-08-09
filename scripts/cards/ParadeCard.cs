namespace PPRogueLite.Cards;

using PPRogueLite.Combat;

public sealed class ParadeCard : CardDefinition
{
    public override string Id => "parade";

    public override string DisplayName => "Parade";

    public override string CardType => "Reaktion";

    public override CardRarity Rarity => CardRarity.Bronze;

    public override string Description => "Du gehst in Deckung. Deine Rüstungsklasse steigt bis zu deinem nächsten Zug um 4.";

    public override string RequirementText => "Kein Wurf nötig";

    public override void Play(CombatEngine engine)
    {
        engine.Player.TempArmorClass += 4;
        engine.Log($"Du gehst in Deckung. Rüstungsklasse vorübergehend {engine.Player.ArmorClass}.", LogTag.System);
    }
}
