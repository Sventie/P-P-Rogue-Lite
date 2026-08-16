namespace PPRogueLite.Cards;

using PPRogueLite.Combat;

/// <summary>
/// Aktionskarte, Fernkampf-Basisangriff des Bogenschützen (Issue #13):
/// löst wie Hieb einen Angriff auf den nächsten Gegner aus (verdeckter W20
/// + GES-Mod gegen RK), aber mit deutlich größerer Reichweite. Die
/// eigentliche Auflösung sitzt in CharacterLoadout.TriggerAbility (Fall
/// "pfeilschuss", Issue #26) - diese Klasse ist nur die Datendefinition der
/// Karte selbst. Startkarte des Bogenschützen (BogenschuetzeDefinition),
/// aber schon jetzt über den Deck-Screen mit anderen Charakteren testbar.
/// </summary>
public sealed class PfeilschussCard : CardDefinition
{
    public override string Id => "pfeilschuss";

    public override string DisplayName => "Pfeilschuss";

    public override string CardType => "Angriff";

    public override CardRarity Rarity => CardRarity.Bronze;

    public override string Description => "Ein gezielter Pfeilschuss aus sicherer Distanz.";

    public override string RequirementText => "W20 + GES gegen RK";

    public override void Play(CombatEngine engine)
    {
        // Altlast-Hook des rundenbasierten Kampfs (siehe CardDefinition) -
        // die Echtzeit-Arena nutzt Play() ohnehin nicht.
    }
}
