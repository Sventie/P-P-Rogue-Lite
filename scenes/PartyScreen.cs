namespace PPRogueLite;

using System.Collections.Generic;
using System.Linq;
using Godot;
using PPRogueLite.Meta;

/// <summary>
/// Gruppenzusammenstellung vor einem Dungeon (Issue #5, über "Gruppe
/// managen" im Hub erreichbar - nur zwischen zwei Dungeons, wie
/// "Karten managen"): zeigt den Hauptcharakter fest an (nicht abwählbar,
/// wird weiterhin direkt gesteuert) sowie alle übrigen besessenen
/// Charaktere als an-/abwählbare Gefährten, bis zu
/// PlayerCharacterCollection.MaxCompanions gleichzeitig. Gefallene
/// Gefährten (Permadeath, Issue #7) werden ausgegraut und sind nicht mehr
/// wählbar. Gleiches "bei jeder Änderung alles neu rendern"-Prinzip wie
/// DeckScreen/Shop.
///
/// Seit dem Charakter-Shop (Issue #6) kann das Roster mehrere Gefährten
/// derselben Klasse enthalten (z. B. zwei Bogenschützen, noch mit
/// identischen Stats - Varianten sind Issue #29) - Panels zeigen dann
/// zusätzlich eine laufende Nummer ("Bogenschütze #1"/"#2"), damit sie in
/// der Liste unterscheidbar bleiben.
/// </summary>
public partial class PartyScreen : Control
{
    private HBoxContainer _rosterContainer = null!;
    private Label _subtitleLabel = null!;

    public override void _Ready()
    {
        _rosterContainer = GetNode<HBoxContainer>("MarginContainer/VBoxContainer/RosterScroll/RosterContainer");
        _subtitleLabel = GetNode<Label>("MarginContainer/VBoxContainer/SubtitleLabel");

        var backButton = GetNode<Button>("MarginContainer/VBoxContainer/BackButton");
        backButton.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");

        RenderAll();
    }

    private void RenderAll()
    {
        foreach (Node child in _rosterContainer.GetChildren())
        {
            child.QueueFree();
        }

        int selectedCount = PlayerCharacterCollection.SelectedCompanions.Count;
        var leaderDefinition = PlayerCharacterCollection.Leader.ClassDefinition;
        _subtitleLabel.Text =
            $"Gefährten: {selectedCount} / {PlayerCharacterCollection.MaxCompanions} — {leaderDefinition.Name} ({leaderDefinition.ClassName}) ist als Hauptcharakter immer dabei.";

        _rosterContainer.AddChild(BuildCharacterPanel(PlayerCharacterCollection.Leader, isLeader: true, suffix: string.Empty));

        var companions = PlayerCharacterCollection.Roster
            .Where(character => character != PlayerCharacterCollection.Leader)
            .ToList();
        var countByClass = companions
            .GroupBy(character => character.ClassDefinition.ClassName)
            .ToDictionary(group => group.Key, group => group.Count());
        var seenByClass = new Dictionary<string, int>();

        foreach (var companion in companions)
        {
            string className = companion.ClassDefinition.ClassName;
            seenByClass.TryGetValue(className, out int seen);
            seen++;
            seenByClass[className] = seen;

            string suffix = countByClass[className] > 1 ? $" #{seen}" : string.Empty;
            _rosterContainer.AddChild(BuildCharacterPanel(companion, isLeader: false, suffix));
        }
    }

    private Control BuildCharacterPanel(OwnedCharacter character, bool isLeader, string suffix)
    {
        var panel = new PanelContainer
        {
            ThemeTypeVariation = "CardPanel",
            CustomMinimumSize = new Vector2(180, 0),
        };

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);

        var definition = character.ClassDefinition;
        vbox.AddChild(new Label { Text = definition.Name, ThemeTypeVariation = "CardTypeLabel" });
        vbox.AddChild(new Label { Text = definition.ClassName + suffix });
        vbox.AddChild(new Label { Text = $"HP {definition.MaxHp} · RK {definition.BaseArmorClass}" });

        if (!character.IsAlive)
        {
            vbox.AddChild(new Label { Text = "Gefallen" });
            panel.Modulate = new Color(1f, 1f, 1f, 0.4f);
        }
        else if (isLeader)
        {
            vbox.AddChild(new Label { Text = "Hauptcharakter" });
        }
        else
        {
            bool selected = PlayerCharacterCollection.SelectedCompanions.Contains(character);
            bool groupFull = PlayerCharacterCollection.SelectedCompanions.Count >= PlayerCharacterCollection.MaxCompanions;

            var button = new Button { Text = selected ? "In der Gruppe" : "Zur Gruppe hinzufügen" };
            button.Disabled = !selected && groupFull;
            button.Pressed += () =>
            {
                if (selected)
                {
                    PlayerCharacterCollection.SelectedCompanions.Remove(character);
                }
                else
                {
                    PlayerCharacterCollection.SelectedCompanions.Add(character);
                }

                RenderAll();
            };
            vbox.AddChild(button);
        }

        panel.AddChild(vbox);
        return panel;
    }
}
