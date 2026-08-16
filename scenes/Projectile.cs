namespace PPRogueLite;

using Godot;
using PPRogueLite.Combat;
using PPRogueLite.Enemies;

/// <summary>
/// Projektil eines Fernkampf-Gegners (Issue #9): reiner Code, keine eigene
/// .tscn nötig (wie DeckStackView) - fliegt geradlinig in eine feste
/// Richtung (kein Homing), Treffer wird per Distanz zum Spieler geprüft
/// (keine Godot-Physik/Kollision, wie überall sonst im Projekt). Löst
/// denselben verdeckten W20-Wurf wie ein Nahkampfangriff aus.
///
/// Zweitverwendung als rein kosmetisches Projektil für Nahkampf-fremde
/// Fähigkeiten (Pfeilschuss/Arkaner Blitz, Issue #13): Cosmetic=true
/// überspringt die komplette Trefferauflösung (die läuft schon instant über
/// CharacterLoadout.ResolveAttack, Issue #26, mit größerer Reichweite statt
/// Flugzeit als eigentlichem Skill-Faktor) - das Projektil fliegt dann nur
/// zur visuellen Rückmeldung bis MaxLifetime oder Bildschirmrand.
/// </summary>
public partial class Projectile : Node2D
{
    private const float Speed = 320f;
    private const float MaxLifetime = 3f;
    private const float HitRadius = 20f;
    private const float DrawRadius = 5f;
    private const float SlowDuration = 2f;
    private const float SlowMultiplier = 0.5f;

    private static readonly Color MissColor = new(0.662745f, 0.603922f, 0.470588f);
    private static readonly Color SlowColor = new(0.45f, 0.25f, 0.55f);

    public Vector2 Direction { get; set; }

    public int AttackBonus { get; set; }

    public int DamageDie { get; set; }

    public int DamageBonus { get; set; }

    public EnemyOnHitEffect OnHitEffect { get; set; }

    public Color Color { get; set; } = Colors.White;

    /// <summary>true = keine Trefferauflösung, nur visueller Flug (Issue #13: Spieler-Fernkampfkarten, siehe Klassenkommentar).</summary>
    public bool Cosmetic { get; set; }

    private float _lifetime;
    private bool _disabled;
    private PackedScene _floatingTextScene = null!;

    public override void _Ready()
    {
        AddToGroup("projectiles");
        _floatingTextScene = GD.Load<PackedScene>("res://scenes/FloatingText.tscn");
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, DrawRadius, Color);
    }

    public override void _Process(double delta)
    {
        if (_disabled)
        {
            return;
        }

        _lifetime += (float)delta;
        if (_lifetime >= MaxLifetime)
        {
            QueueFree();
            return;
        }

        Position += Direction * Speed * (float)delta;

        if (Cosmetic)
        {
            return;
        }

        var target = this.FindNearestPartyMember(Position);
        if (target is not null && Position.DistanceTo(target.Position) <= HitRadius)
        {
            ResolveHit(target);
            QueueFree();
        }
    }

    public void SetDisabled(bool disabled)
    {
        _disabled = disabled;
    }

    private void ResolveHit(IPartyMember target)
    {
        int roll = Dice.Roll(20);
        int total = roll + AttackBonus;
        bool hit = total >= target.EffectiveArmorClass;

        if (!hit)
        {
            SpawnFloatingText(MissColor, "Verfehlt");
            return;
        }

        int damage = Dice.Roll(DamageDie) + DamageBonus;
        target.TakeDamage(damage);
        SpawnFloatingText(Color, damage.ToString());

        if (OnHitEffect == EnemyOnHitEffect.Slow)
        {
            target.ApplySlow(SlowDuration, SlowMultiplier);
            SpawnFloatingText(SlowColor, "Verlangsamt!");
        }
    }

    private void SpawnFloatingText(Color color, string text)
    {
        var floatingText = _floatingTextScene.Instantiate<FloatingText>();
        GetParent()?.AddChild(floatingText);
        floatingText.GlobalPosition = GlobalPosition;
        floatingText.ShowText(text, color);
    }
}
