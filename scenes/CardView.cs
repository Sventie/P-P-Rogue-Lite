namespace PPRogueLite;

using System;
using Godot;
using PPRogueLite.Cards;

/// <summary>
/// Wiederverwendbare Kartenansicht: zeigt Typ/Name/Beschreibung/Anforderung
/// direkt auf der Karte (keine Hover-Info nötig) und optional eine Stückzahl
/// (z. B. "×3" bei mehreren gleichen Karten im Deck-Screen).
///
/// Unterstützt wahlweise Klicks auf die ganze Karte (Kampf: Karte spielen,
/// über <see cref="Clicked"/>) oder ein kleines Action-Badge oben rechts
/// (Deck-Screen: Karte per "×"/"+" zwischen Deck und Bestand verschieben,
/// über <see cref="ShowAction"/>/<see cref="ActionClicked"/>).
/// </summary>
public partial class CardView : Control
{
    private static readonly Color DisabledModulate = new(1f, 1f, 1f, 0.5f);

    public event Action? Clicked;

    public event Action? ActionClicked;

    public CardDefinition? Card { get; private set; }

    private Label _typeLabel = null!;
    private Label _nameLabel = null!;
    private Label _descriptionLabel = null!;
    private Label _requirementLabel = null!;
    private Button _actionButton = null!;

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
        _typeLabel = GetNode<Label>("Panel/CardScroll/CardVBox/TypeLabel");
        _nameLabel = GetNode<Label>("Panel/CardScroll/CardVBox/NameLabel");
        _descriptionLabel = GetNode<Label>("Panel/CardScroll/CardVBox/DescriptionLabel");
        _requirementLabel = GetNode<Label>("Panel/CardScroll/CardVBox/RequirementLabel");
        _actionButton = GetNode<Button>("ActionButton");
        _actionButton.Pressed += () => ActionClicked?.Invoke();
    }

    public void Populate(CardDefinition card, int count = 1)
    {
        Card = card;
        _typeLabel.Text = card.CardType.ToUpperInvariant();
        _nameLabel.Text = count > 1 ? $"{card.DisplayName}  ×{count}" : card.DisplayName;
        _descriptionLabel.Text = card.Description;
        _requirementLabel.Text = card.RequirementText;
    }

    /// <summary>Zeigt das kleine Action-Badge oben rechts mit dem gegebenen Symbol (z. B. "×" oder "+").</summary>
    public void ShowAction(string symbol)
    {
        _actionButton.Visible = true;
        _actionButton.Text = symbol;
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
}
