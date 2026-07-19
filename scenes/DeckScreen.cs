namespace PPRogueLite;

using System.Collections.Generic;
using Godot;
using PPRogueLite.Cards;
using PPRogueLite.Meta;

/// <summary>
/// Deck-Screen: links das aktuelle Kampf-Deck, rechts der Kartenbestand,
/// der nicht im Deck ist. Reine Anzeige - Karten sind (noch) nicht
/// klickbar/verschiebbar, siehe CLAUDE.md für die geplante Interaktion.
/// </summary>
public partial class DeckScreen : Control
{
    public override void _Ready()
    {
        var deckHeaderLabel = GetNode<Label>("MarginContainer/VBoxContainer/ColumnsRow/DeckColumn/DeckVBox/DeckHeaderLabel");
        var deckGrid = GetNode<GridContainer>("MarginContainer/VBoxContainer/ColumnsRow/DeckColumn/DeckVBox/DeckScroll/DeckGrid");
        var benchHeaderLabel = GetNode<Label>("MarginContainer/VBoxContainer/ColumnsRow/BenchColumn/BenchVBox/BenchHeaderLabel");
        var benchGrid = GetNode<GridContainer>("MarginContainer/VBoxContainer/ColumnsRow/BenchColumn/BenchVBox/BenchScroll/BenchGrid");
        var backButton = GetNode<Button>("MarginContainer/VBoxContainer/BackButton");

        backButton.Pressed += OnBackPressed;

        deckHeaderLabel.Text = $"Im Deck ({PlayerCardCollection.DeckCards.Count})";
        benchHeaderLabel.Text = $"Nicht im Deck ({PlayerCardCollection.BenchCards.Count})";

        RenderColumn(deckGrid, PlayerCardCollection.DeckCards);
        RenderColumn(benchGrid, PlayerCardCollection.BenchCards);
    }

    private static void RenderColumn(GridContainer grid, List<CardDefinition> cards)
    {
        foreach (var card in cards)
        {
            var button = new Button
            {
                Text = card.DisplayName,
                TooltipText = $"{card.CardType}\n{card.Description}\n{card.RequirementText}",
                Disabled = true,
                CustomMinimumSize = new Vector2(150, 70),
                ThemeTypeVariation = "CardButton",
            };
            grid.AddChild(button);
        }
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");
    }
}
