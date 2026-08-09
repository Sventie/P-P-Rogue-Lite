namespace PPRogueLite.Cards;

using PPRogueLite.Combat;

/// <summary>
/// Aktionskarte (Issue #21): mischt beim Auslösen die komplette Ablage
/// zurück in den Nachziehstapel (Deck.ReshuffleDiscardIntoDrawPile) - macht
/// über die Level-up-Kartenwahl (Issue #15) dauerhaft verworfene Karten
/// wieder erreichbar. Bewusst mit langem Cooldown (siehe Player.CooldownFor)
/// als seltener, build-prägender Effekt statt eines häufigen Basis-Tools.
/// Kein Effekt bei leerer Ablage (siehe Player.TriggerAbility).
/// </summary>
public sealed class WiederkehrCard : CardDefinition
{
    public override string Id => "wiederkehr";

    public override string DisplayName => "Wiederkehr";

    public override string CardType => "Fähigkeit";

    public override CardRarity Rarity => CardRarity.Diamant;

    public override string Description => "Deine verworfenen Karten kehren zurück. Mischt die komplette Ablage zurück in deinen Nachziehstapel.";

    public override string RequirementText => "Kein Effekt bei leerer Ablage";

    public override void Play(CombatEngine engine)
    {
        // Altlast-Hook des rundenbasierten Kampfs (siehe CardDefinition) -
        // die Echtzeit-Arena nutzt Play() ohnehin nicht.
    }
}
