namespace PPRogueLite.Cards;

using PPRogueLite.Combat;

/// <summary>
/// Modifikatorkarte, gekoppelte Spielart (Issue #12): hängt an einer
/// bestimmten Aktionskarte (hier Atem holen) und löst zusätzlich zu deren
/// normalem Effekt etwas aus. Die eigentliche Wirkung (AOE-Schaden beim
/// Heilen) sitzt in CharacterLoadout.TriggerAbility (Fall "atemholen",
/// Issue #26) - diese Klasse ist nur die Datendefinition der Karte selbst.
/// </summary>
public sealed class ExplosiveHeilungCard : CardDefinition
{
    public override string Id => "explosive_heilung";

    public override string DisplayName => "Explosive Heilung";

    public override string CardType => "Modifikator";

    public override CardKind Kind => CardKind.Modifier;

    public override string CoupledCardId => "atemholen";

    public override CardRarity Rarity => CardRarity.Platin;

    public override string Description => "Deine Heilung entlädt sich nach außen. Atem holen verursacht zusätzlich Schaden an Gegnern in der Nähe.";

    public override string RequirementText => "Gekoppelt an Atem holen";

    public override void Play(CombatEngine engine)
    {
        // Altlast-Hook des rundenbasierten Kampfs (siehe CardDefinition) -
        // Modifikatorkarten gab es dort nicht, die Echtzeit-Arena nutzt
        // Play() ohnehin nicht.
    }
}
