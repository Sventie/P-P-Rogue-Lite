namespace PPRogueLite.Cards;

using PPRogueLite.Combat;

/// <summary>
/// Aktionskarte, Fernkampf-Basisangriff des Magiers (Issue #13): wie
/// Pfeilschuss ein Angriff auf den nächsten Gegner mit
/// Player.RangedAttackRange statt Player.MeleeRange, aber INT- statt
/// GES-basiert. Die eigentliche Auflösung sitzt in Player.TriggerAbility
/// (Fall "arkaner_blitz") - diese Klasse ist nur die Datendefinition der
/// Karte selbst. Startkarte des Magiers (MagierDefinition), aber schon
/// jetzt über den Deck-Screen mit dem Krieger testbar.
/// </summary>
public sealed class ArkanerBlitzCard : CardDefinition
{
    public override string Id => "arkaner_blitz";

    public override string DisplayName => "Arkaner Blitz";

    public override string CardType => "Angriff";

    public override CardRarity Rarity => CardRarity.Bronze;

    public override string Description => "Ein Blitz arkaner Energie schießt auf den nächsten Gegner.";

    public override string RequirementText => "W20 + INT gegen RK";

    public override void Play(CombatEngine engine)
    {
        // Altlast-Hook des rundenbasierten Kampfs (siehe CardDefinition) -
        // die Echtzeit-Arena nutzt Play() ohnehin nicht.
    }
}
