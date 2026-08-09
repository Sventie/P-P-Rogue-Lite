namespace PPRogueLite.Cards;

using PPRogueLite.Combat;

/// <summary>
/// Modifikatorkarte, gekoppelte Spielart (Issue #12), zweite Nutzung von
/// Damage-over-Time (Issue #23): hängt an Wuchtschlag und entzündet das
/// getroffene Ziel. Im Gegensatz zu Gift stackt eine erneute Anwendung die
/// Intensität (Schaden pro Tick steigt, siehe Enemy.ApplyBurn), statt nur
/// die Dauer zu erneuern. Die eigentliche Wirkung sitzt in
/// Player.TriggerAbility/ApplyCoupledEffect (Fall "wuchtschlag"/"brand") -
/// diese Klasse ist nur die Datendefinition der Karte selbst.
/// </summary>
public sealed class BrandCard : CardDefinition
{
    public override string Id => "brand";

    public override string DisplayName => "Brand";

    public override string CardType => "Modifikator";

    public override CardKind Kind => CardKind.Modifier;

    public override string CoupledCardId => "wuchtschlag";

    public override string Description => "Dein Wuchtschlag entfacht Flammen. Trifft er, entzündet er das Ziel - wiederholte Treffer lassen das Feuer stärker brennen.";

    public override string RequirementText => "Gekoppelt an Wuchtschlag";

    public override void Play(CombatEngine engine)
    {
        // Altlast-Hook des rundenbasierten Kampfs (siehe CardDefinition) -
        // Modifikatorkarten gab es dort nicht, die Echtzeit-Arena nutzt
        // Play() ohnehin nicht.
    }
}
