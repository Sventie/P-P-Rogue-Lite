namespace PPRogueLite;

using System.Collections.Generic;
using System.Linq;
using Godot;
using PPRogueLite.Cards;
using PPRogueLite.Meta;

/// <summary>
/// Deck-Screen: links das aktuelle Kampf-Deck, rechts der Kartenbestand, der
/// nicht im Deck ist. Gleiche Kartentypen werden gruppiert mit Stückzahl
/// ("×3") gezeigt. Karten lassen sich per Drag&amp;Drop zwischen den beiden
/// Spalten verschieben (Ziel wird über die Mausposition beim Drop bestimmt,
/// nicht über eigene Drop-Zonen-Klassen).
///
/// Das Deck muss beim Verlassen genau 10 Karten enthalten: "Zurück zum Hub"
/// ist deaktiviert, solange die Anzahl abweicht; bei mehr als 10 Karten wird
/// die Anzeige zusätzlich rot.
/// </summary>
public partial class DeckScreen : Control
{
    private const int RequiredDeckSize = 10;

    private static readonly Color BadColor = new(0.611765f, 0.231373f, 0.231373f);

    private PackedScene _cardViewScene = null!;
    private Label _deckHeaderLabel = null!;
    private GridContainer _deckGrid = null!;
    private PanelContainer _deckColumn = null!;
    private Label _benchHeaderLabel = null!;
    private GridContainer _benchGrid = null!;
    private PanelContainer _benchColumn = null!;
    private Label _deckStatusLabel = null!;
    private Button _backButton = null!;

    public override void _Ready()
    {
        _cardViewScene = GD.Load<PackedScene>("res://scenes/CardView.tscn");

        _deckColumn = GetNode<PanelContainer>("MarginContainer/VBoxContainer/ColumnsRow/DeckColumn");
        _deckHeaderLabel = GetNode<Label>("MarginContainer/VBoxContainer/ColumnsRow/DeckColumn/DeckVBox/DeckHeaderLabel");
        _deckGrid = GetNode<GridContainer>("MarginContainer/VBoxContainer/ColumnsRow/DeckColumn/DeckVBox/DeckScroll/DeckGrid");

        _benchColumn = GetNode<PanelContainer>("MarginContainer/VBoxContainer/ColumnsRow/BenchColumn");
        _benchHeaderLabel = GetNode<Label>("MarginContainer/VBoxContainer/ColumnsRow/BenchColumn/BenchVBox/BenchHeaderLabel");
        _benchGrid = GetNode<GridContainer>("MarginContainer/VBoxContainer/ColumnsRow/BenchColumn/BenchVBox/BenchScroll/BenchGrid");

        _deckStatusLabel = GetNode<Label>("MarginContainer/VBoxContainer/DeckStatusLabel");
        _deckStatusLabel.AddThemeColorOverride("font_color", BadColor);

        _backButton = GetNode<Button>("MarginContainer/VBoxContainer/BackButton");
        _backButton.Pressed += OnBackPressed;

        RenderAll();
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        return data.VariantType == Variant.Type.Dictionary;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var payload = data.AsGodotDictionary();
        if (!payload.ContainsKey("cardId") || !payload.ContainsKey("source"))
        {
            return;
        }

        string cardId = payload["cardId"].AsString();
        string source = payload["source"].AsString();

        Vector2 mousePosition = GetGlobalMousePosition();
        bool droppedOnDeck = _deckColumn.GetGlobalRect().HasPoint(mousePosition);
        bool droppedOnBench = _benchColumn.GetGlobalRect().HasPoint(mousePosition);

        if (droppedOnDeck && source != "deck")
        {
            MoveCard(cardId, PlayerCardCollection.BenchCards, PlayerCardCollection.DeckCards);
        }
        else if (droppedOnBench && source != "bench")
        {
            MoveCard(cardId, PlayerCardCollection.DeckCards, PlayerCardCollection.BenchCards);
        }
    }

    private void MoveCard(string cardId, List<CardDefinition> from, List<CardDefinition> to)
    {
        var card = from.FirstOrDefault(c => c.Id == cardId);
        if (card is null)
        {
            return;
        }

        from.Remove(card);
        to.Add(card);
        RenderAll();
    }

    private void RenderAll()
    {
        RenderColumn(_deckGrid, PlayerCardCollection.DeckCards, "deck");
        RenderColumn(_benchGrid, PlayerCardCollection.BenchCards, "bench");
        UpdateDeckStatus();
    }

    private void RenderColumn(GridContainer grid, List<CardDefinition> cards, string source)
    {
        foreach (Node child in grid.GetChildren())
        {
            child.QueueFree();
        }

        var grouped = cards
            .GroupBy(card => card.Id)
            .Select(group => (Card: group.First(), Count: group.Count()))
            .OrderBy(entry => entry.Card.DisplayName);

        foreach (var (card, count) in grouped)
        {
            var cardView = _cardViewScene.Instantiate<CardView>();
            cardView.Draggable = true;
            cardView.Source = source;
            grid.AddChild(cardView);
            cardView.Populate(card, count);
        }
    }

    private void UpdateDeckStatus()
    {
        int deckCount = PlayerCardCollection.DeckCards.Count;
        int benchCount = PlayerCardCollection.BenchCards.Count;

        _deckHeaderLabel.Text = $"Im Deck ({deckCount})";
        _benchHeaderLabel.Text = $"Nicht im Deck ({benchCount})";

        bool tooMany = deckCount > RequiredDeckSize;
        if (tooMany)
        {
            _deckHeaderLabel.AddThemeColorOverride("font_color", BadColor);
        }
        else
        {
            _deckHeaderLabel.RemoveThemeColorOverride("font_color");
        }

        bool exact = deckCount == RequiredDeckSize;
        _backButton.Disabled = !exact;
        _deckStatusLabel.Visible = !exact;

        if (!exact)
        {
            _deckStatusLabel.Text = tooMany
                ? $"Zu viele Karten im Deck ({deckCount}/{RequiredDeckSize}). Lege welche ab, um zurückzukehren."
                : $"Zu wenige Karten im Deck ({deckCount}/{RequiredDeckSize}). Füge welche hinzu, um zurückzukehren.";
        }
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");
    }
}
