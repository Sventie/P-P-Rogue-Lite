namespace PPRogueLite.Cards;

using PPRogueLite.Combat;

/// <summary>
/// Aktionskarte (Issue #22), gezielte Alternative zu Wiederkehr (#21):
/// löst beim Auslösen eine Auswahl aus (Player.DiscardChoiceOffered) -
/// gleiches CardView-Klick-Auswahlprinzip wie die Level-up-Kartenwahl
/// (Issue #15), hier aber aus der Ablage statt frisch gezogenen Karten. Die
/// gewählte Karte wird direkt ausgerüstet (Player.ResolveDiscardChoice),
/// alle anderen bleiben in der Ablage. Bewusst mit langem Cooldown (siehe
/// Player.CooldownFor) als seltener, build-prägender Effekt. Kein Effekt
/// bei leerer Ablage (siehe Player.TriggerAbility).
/// </summary>
public sealed class ErinnerungCard : CardDefinition
{
    public override string Id => "erinnerung";

    public override string DisplayName => "Erinnerung";

    public override string CardType => "Fähigkeit";

    public override string Description => "Du erinnerst dich an eine verworfene Karte. Wähle eine Karte aus deiner Ablage und rüste sie erneut aus.";

    public override string RequirementText => "Kein Effekt bei leerer Ablage";

    public override void Play(CombatEngine engine)
    {
        // Altlast-Hook des rundenbasierten Kampfs (siehe CardDefinition) -
        // die Echtzeit-Arena nutzt Play() ohnehin nicht.
    }
}
