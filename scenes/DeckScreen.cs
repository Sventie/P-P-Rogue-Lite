namespace PPRogueLite;

using System;
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
/// Das Deck muss beim Verlassen zwischen MinDeckSize und MaxDeckSize Karten
/// enthalten: "Zurück zum Hub" ist deaktiviert, solange die Anzahl außerhalb
/// liegt; bei mehr als MaxDeckSize Karten wird die Anzeige zusätzlich rot.
/// MinDeckSize ist bewusst auf 1 (statt fest 10) gesenkt, damit sich auch
/// kleinere Testdecks zusammenstellen lassen (z. B. nur die neuen
/// Modifikatorkarten, um sie isoliert zu testen).
/// </summary>
public partial class DeckScreen : Control
{
    private const int MinDeckSize = 1;
    private const int MaxDeckSize = 10;
    private const float CardWidth = 190f;

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

        SetupResponsiveColumns(_deckGrid);
        SetupResponsiveColumns(_benchGrid);

        RenderAll();
    }

    /// <summary>
    /// GridContainer hat keine "so viele Spalten wie passen"-Option, daher
    /// wird die Spaltenzahl aus der verfügbaren Breite berechnet und bei
    /// jeder Größenänderung (z. B. Fenster-Resize) neu bestimmt.
    /// </summary>
    private static void SetupResponsiveColumns(GridContainer grid)
    {
        grid.Resized += () => ApplyColumnCount(grid);
        ApplyColumnCount(grid);
    }

    private static void ApplyColumnCount(GridContainer grid)
    {
        float separation = grid.GetThemeConstant("h_separation");
        int columns = Math.Max(1, (int)((grid.Size.X + separation) / (CardWidth + separation)));

        if (grid.Columns != columns)
        {
            grid.Columns = columns;
        }
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

        bool tooMany = deckCount > MaxDeckSize;
        if (tooMany)
        {
            _deckHeaderLabel.AddThemeColorOverride("font_color", BadColor);
        }
        else
        {
            _deckHeaderLabel.RemoveThemeColorOverride("font_color");
        }

        bool valid = deckCount >= MinDeckSize && deckCount <= MaxDeckSize;
        _backButton.Disabled = !valid;
        _deckStatusLabel.Visible = !valid;

        if (!valid)
        {
            _deckStatusLabel.Text = tooMany
                ? $"Zu viele Karten im Deck ({deckCount}/{MaxDeckSize}). Lege welche ab, um zurückzukehren."
                : $"Zu wenige Karten im Deck ({deckCount}). Mindestens {MinDeckSize} Karte nötig, um zurückzukehren.";
        }
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");
    }
}
