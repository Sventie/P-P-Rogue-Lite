namespace PPRogueLite;

using System;
using Godot;
using PPRogueLite.Cards;

/// <summary>
/// Wiederverwendbare Kartenansicht: zeigt Typ/Name/Beschreibung/Anforderung
/// direkt auf der Karte (keine Hover-Info nötig) und optional eine Stückzahl
/// (z. B. "×3" bei mehreren gleichen Karten im Deck-Screen).
///
/// Unterstützt wahlweise Klicks (Kampf: Karte spielen, über <see cref="Clicked"/>)
/// oder Drag&amp;Drop (Deck-Screen: Karte zwischen Deck/Bestand verschieben,
/// über <see cref="Draggable"/>/<see cref="_GetDragData"/>).
/// </summary>
public partial class CardView : PanelContainer
{
    private static readonly Color DisabledModulate = new(1f, 1f, 1f, 0.5f);

    public event Action? Clicked;

    public bool Draggable { get; set; }

    /// <summary>Frei wählbare Herkunftskennung fürs Drag&amp;Drop, z. B. "deck"/"bench".</summary>
    public string Source { get; set; } = string.Empty;

    public CardDefinition? Card { get; private set; }

    private Label _typeLabel = null!;
    private Label _nameLabel = null!;
    private Label _descriptionLabel = null!;
    private Label _requirementLabel = null!;

    private bool _disabled;

    public bool Disabled
    {
        get => _disabled;
        set
        {
            _disabled = value;
            Modulate = value ? DisabledModulate : Colors.White;
        }
    }

    public override void _Ready()
    {
        _typeLabel = GetNode<Label>("CardVBox/TypeLabel");
        _nameLabel = GetNode<Label>("CardVBox/NameLabel");
        _descriptionLabel = GetNode<Label>("CardVBox/DescriptionLabel");
        _requirementLabel = GetNode<Label>("CardVBox/RequirementLabel");
    }

    public void Populate(CardDefinition card, int count = 1)
    {
        Card = card;
        _typeLabel.Text = card.CardType.ToUpperInvariant();
        _nameLabel.Text = count > 1 ? $"{card.DisplayName}  ×{count}" : card.DisplayName;
        _descriptionLabel.Text = card.Description;
        _requirementLabel.Text = card.RequirementText;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (_disabled)
        {
            return;
        }

        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
        {
            Clicked?.Invoke();
        }
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (!Draggable || Card is null)
        {
            return default;
        }

        var preview = new Label { Text = Card.DisplayName };
        SetDragPreview(preview);

        return new Godot.Collections.Dictionary
        {
            ["cardId"] = Card.Id,
            ["source"] = Source,
        };
    }
}
