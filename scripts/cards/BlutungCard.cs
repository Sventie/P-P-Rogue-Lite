namespace PPRogueLite.Cards;

using PPRogueLite.Combat;

/// <summary>
/// Modifikatorkarte, gekoppelte Spielart (Issue #12), dritte Nutzung von
/// Damage-over-Time (Issue #24): hängt an Hieb (zusätzlich zu Giftklinge -
/// mehrere Modifikatoren können an derselben Karte hängen, siehe
/// Player._modifiersByTargetCardId) und lässt das getroffene Ziel bluten.
/// Der Schaden pro Tick ist ein Prozentsatz der maximalen HP des Ziels
/// statt eines festen Werts (siehe Enemy.ApplyBleed) - effektiv gegen
/// tanky Ziele. Die eigentliche Wirkung sitzt in Player.TriggerAbility/
/// ApplyCoupledEffect (Fall "hieb"/"blutung") - diese Klasse ist nur die
/// Datendefinition der Karte selbst.
/// </summary>
public sealed class BlutungCard : CardDefinition
{
    public override string Id => "blutung";

    public override string DisplayName => "Blutung";

    public override string CardType => "Modifikator";

    public override CardKind Kind => CardKind.Modifier;

    public override string CoupledCardId => "hieb";

    public override string Description => "Dein Hieb reißt tiefe Wunden. Das getroffene Ziel blutet - der Schaden richtet sich nach seiner maximalen Lebenskraft, wirkt also besonders gegen zähe Gegner.";

    public override string RequirementText => "Gekoppelt an Hieb";

    public override void Play(CombatEngine engine)
    {
        // Altlast-Hook des rundenbasierten Kampfs (siehe CardDefinition) -
        // Modifikatorkarten gab es dort nicht, die Echtzeit-Arena nutzt
        // Play() ohnehin nicht.
    }
}
