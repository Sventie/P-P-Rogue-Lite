namespace PPRogueLite;

using Godot;

/// <summary>
/// Kurzer Flugtext für Wurf-Ergebnisse in der Echtzeit-Arena (Schaden,
/// "Verfehlt", Heilung, Buff-Namen) - ersetzt die Popup/Log-Anzeige aus dem
/// alten rundenbasierten Kampf. Steigt kurz auf, verblasst, entfernt sich
/// danach selbst.
/// </summary>
public partial class FloatingText : Node2D
{
    private const float RiseDistance = 40f;
    private const double Duration = 0.6;

    private Label _label = null!;

    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
    }

    public void ShowText(string text, Color color)
    {
        _label.Text = text;
        _label.AddThemeColorOverride("font_color", color);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(this, "position:y", Position.Y - RiseDistance, Duration);
        tween.TweenProperty(_label, "modulate:a", 0.0f, Duration);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }
}
