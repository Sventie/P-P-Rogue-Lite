namespace PPRogueLite;

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

        _rosterContainer.AddChild(BuildCharacterPanel(PlayerCharacterCollection.Leader, isLeader: true));

        foreach (var companion in PlayerCharacterCollection.Roster.Where(character => character != PlayerCharacterCollection.Leader))
        {
            _rosterContainer.AddChild(BuildCharacterPanel(companion, isLeader: false));
        }
    }

    private Control BuildCharacterPanel(OwnedCharacter character, bool isLeader)
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
        vbox.AddChild(new Label { Text = definition.ClassName });
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
