namespace PPRogueLite.Cards;

using PPRogueLite.Combat;

public sealed class AtemHolenCard : CardDefinition
{
    public override string Id => "atemholen";

    public override string DisplayName => "Atem holen";

    public override string CardType => "Erholung";

    public override CardRarity Rarity => CardRarity.Silber;

    public override string Description => "Ein Moment der Sammlung. Einmal pro Kampf: heile dich.";

    public override string RequirementText => "W10 + Stufe (einmalig)";

    public override bool IsExhaust => true;

    public override void Play(CombatEngine engine)
    {
        int heal = Dice.Roll(10) + 1;
        engine.Player.Heal(heal);
        engine.Log($"Du holst Atem und heilst [b]{heal}[/b] Trefferpunkte.", LogTag.Good);
    }
}
