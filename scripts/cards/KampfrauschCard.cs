namespace PPRogueLite.Cards;

using PPRogueLite.Combat;

/// <summary>
/// Modifikatorkarte, globale Spielart (Issue #12): dauerhafter passiver
/// Bonus, kein eigener Cooldown-Slot. Die eigentliche Wirkung (+1 auf
/// Angriffswürfe) sitzt in CharacterLoadout.TriggerAbility (Hieb/
/// Wuchtschlag, Issue #26) - diese Klasse ist nur die Datendefinition der
/// Karte selbst.
/// </summary>
public sealed class KampfrauschCard : CardDefinition
{
    public override string Id => "kampfrausch";

    public override string DisplayName => "Kampfrausch";

    public override string CardType => "Modifikator";

    public override CardKind Kind => CardKind.Modifier;

    public override CardRarity Rarity => CardRarity.Gold;

    public override string Description => "Du kämpfst mit wilder Entschlossenheit. Dauerhaft +1 auf alle Angriffswürfe.";

    public override string RequirementText => "Passiv, kein Wurf nötig";

    public override void Play(CombatEngine engine)
    {
        // Altlast-Hook des rundenbasierten Kampfs (siehe CardDefinition) -
        // Modifikatorkarten gab es dort nicht, die Echtzeit-Arena nutzt
        // Play() ohnehin nicht.
    }
}
