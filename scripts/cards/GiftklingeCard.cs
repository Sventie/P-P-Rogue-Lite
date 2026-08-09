namespace PPRogueLite.Cards;

using PPRogueLite.Combat;

/// <summary>
/// Modifikatorkarte, gekoppelte Spielart (Issue #12), erste Nutzung des
/// Gift-Status-Effekts (Issue #14): hängt an Hieb und vergiftet das
/// getroffene Ziel mit einer Chance pro Treffer. Die eigentliche Wirkung
/// (Chance-Wurf + Enemy.ApplyPoison) sitzt in Player.TriggerAbility/
/// ApplyCoupledEffect (Fall "hieb"/"giftklinge") - diese Klasse ist nur die
/// Datendefinition der Karte selbst.
/// </summary>
public sealed class GiftklingeCard : CardDefinition
{
    public override string Id => "giftklinge";

    public override string DisplayName => "Giftklinge";

    public override string CardType => "Modifikator";

    public override CardKind Kind => CardKind.Modifier;

    public override string CoupledCardId => "hieb";

    public override string Description => "Deine Klinge trieft von Gift. Hieb hat eine Chance, das getroffene Ziel zu vergiften und über Zeit Schaden zuzufügen.";

    public override string RequirementText => "Gekoppelt an Hieb";

    public override void Play(CombatEngine engine)
    {
        // Altlast-Hook des rundenbasierten Kampfs (siehe CardDefinition) -
        // Modifikatorkarten gab es dort nicht, die Echtzeit-Arena nutzt
        // Play() ohnehin nicht.
    }
}
