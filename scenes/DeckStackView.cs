namespace PPRogueLite;

using Godot;

/// <summary>
/// Stapel-Visualisierung für einen Kartenstapel im Level-up-Screen: zeichnet
/// eine Karten-Rückseite pro noch vorhandener Karte (leicht versetzt
/// übereinander) - wird also sichtbar kleiner, bis der Stapel leer ist.
/// </summary>
public partial class DeckStackView : Control
{
    private const float CardWidth = 60f;
    private const float CardHeight = 84f;
    private const float Offset = 3f;
    private const int MaxVisibleCards = 10;

    private static readonly Color CardColor = new(0.847059f, 0.796078f, 0.627451f);
    private static readonly Color BorderColor = new(0f, 0f, 0f, 0.35f);

    private int _count;

    public int Count
    {
        get => _count;
        set
        {
            _count = value;
            CustomMinimumSize = ComputeSize();
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        int visible = Mathf.Clamp(_count, 0, MaxVisibleCards);

        for (int i = visible - 1; i >= 0; i--)
        {
            var topLeft = new Vector2(i * Offset, i * Offset);
            var rect = new Rect2(topLeft, new Vector2(CardWidth, CardHeight));
            DrawRect(rect, CardColor);
            DrawRect(rect, BorderColor, false, 1.5f);
        }
    }

    private Vector2 ComputeSize()
    {
        int visible = Mathf.Clamp(_count, 1, MaxVisibleCards);
        return new Vector2(CardWidth + ((visible - 1) * Offset), CardHeight + ((visible - 1) * Offset));
    }
}
