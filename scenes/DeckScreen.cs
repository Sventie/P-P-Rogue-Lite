namespace PPRogueLite;

using System.Collections.Generic;
using System.Linq;
using Godot;
using PPRogueLite.Cards;
using PPRogueLite.Meta;

/// <summary>
/// Deck-Screen: links das aktuelle Kampf-Deck, rechts der Kartenbestand, der
/// nicht im Deck ist. Gleiche Kartentypen werden gruppiert mit Stückzahl
/// ("×3") gezeigt. Jede Karte hat ein kleines Action-Badge oben rechts: "×"
/// im Deck (entfernt eine Karteninstanz aus dem Deck, legt sie in den
/// Bestand), "+" im Bestand (nimmt eine Karteninstanz ins Deck auf).
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
    private Label _benchHeaderLabel = null!;
    private GridContainer _benchGrid = null!;
    private Label _deckStatusLabel = null!;
    private Button _backButton = null!;

    public override void _Ready()
    {
        _cardViewScene = GD.Load<PackedScene>("res://scenes/CardView.tscn");

        _deckHeaderLabel = GetNode<Label>("MarginContainer/VBoxContainer/ColumnsRow/DeckColumnWrapper/DeckHeaderLabel");
        _deckGrid = GetNode<GridContainer>("MarginContainer/VBoxContainer/ColumnsRow/DeckColumnWrapper/DeckColumn/DeckScroll/DeckGrid");

        _benchHeaderLabel = GetNode<Label>("MarginContainer/VBoxContainer/ColumnsRow/BenchColumnWrapper/BenchHeaderLabel");
        _benchGrid = GetNode<GridContainer>("MarginContainer/VBoxContainer/ColumnsRow/BenchColumnWrapper/BenchColumn/BenchScroll/BenchGrid");

        _deckStatusLabel = GetNode<Label>("MarginContainer/VBoxContainer/DeckStatusLabel");
        _deckStatusLabel.AddThemeColorOverride("font_color", BadColor);

        _backButton = GetNode<Button>("MarginContainer/VBoxContainer/BackButton");
        _backButton.Pressed += OnBackPressed;

        RenderAll();
    }

    private void MoveOneCard(string cardId, List<CardDefinition> from, List<CardDefinition> to)
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
        RenderColumn(_deckGrid, "×", from: PlayerCardCollection.DeckCards, to: PlayerCardCollection.BenchCards);
        RenderColumn(_benchGrid, "+", from: PlayerCardCollection.BenchCards, to: PlayerCardCollection.DeckCards);
        UpdateDeckStatus();
    }

    private void RenderColumn(GridContainer grid, string actionSymbol, List<CardDefinition> from, List<CardDefinition> to)
    {
        foreach (Node child in grid.GetChildren())
        {
            child.QueueFree();
        }

        var grouped = from
            .GroupBy(card => card.Id)
            .Select(group => (Card: group.First(), Count: group.Count()))
            .OrderBy(entry => entry.Card.DisplayName);

        foreach (var (card, count) in grouped)
        {
            var cardView = _cardViewScene.Instantiate<CardView>();
            grid.AddChild(cardView);
            cardView.Populate(card, count);
            cardView.ShowAction(actionSymbol);
            cardView.ActionClicked += () => MoveOneCard(card.Id, from, to);
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
