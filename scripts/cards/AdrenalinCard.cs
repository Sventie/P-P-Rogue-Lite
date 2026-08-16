namespace PPRogueLite.Cards;

using PPRogueLite.Combat;

/// <summary>
/// Modifikatorkarte, reaktive Spielart (Issue #12): reagiert auf ein
/// Ereignis (hier ein kritischer Treffer) statt auf Cooldown oder an eine
/// andere Karte gekoppelt zu sein. Die eigentliche Wirkung (Tempo-Buff nach
/// Krit) sitzt in CharacterLoadout.ResolveAttack/OnCriticalHit (Issue #26)
/// - diese Klasse ist nur die Datendefinition der Karte selbst.
/// </summary>
public sealed class AdrenalinCard : CardDefinition
{
    public override string Id => "adrenalin";

    public override string DisplayName => "Adrenalin";

    public override string CardType => "Modifikator";

    public override CardKind Kind => CardKind.Modifier;

    public override CardRarity Rarity => CardRarity.Platin;

    public override string Description => "Ein kritischer Treffer setzt Adrenalin frei. Nach einem kritischen Treffer bist du für kurze Zeit schneller.";

    public override string RequirementText => "Reagiert auf kritische Treffer";

    public override void Play(CombatEngine engine)
    {
        // Altlast-Hook des rundenbasierten Kampfs (siehe CardDefinition) -
        // Modifikatorkarten gab es dort nicht, die Echtzeit-Arena nutzt
        // Play() ohnehin nicht.
    }
}
