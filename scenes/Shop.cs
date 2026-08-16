namespace PPRogueLite;

using System.Collections.Generic;
using System.Linq;
using Godot;
using PPRogueLite.Cards;
using PPRogueLite.Character;
using PPRogueLite.Meta;
using PPRogueLite.Shop;

/// <summary>
/// Shop (Issue #4), nur über die Taverne erreichbar (Hub.tscn - ersetzt
/// den bisherigen "Mit Loot entkommen"-Platzhalter, siehe Hub.cs), nicht
/// über das Lager zwischen zwei Stages. Drei Bereiche:
///
/// - Sonderangebote: 3 einzeln kaufbare, konkrete Karten (ShopState,
///   würfeln sich neu bei jedem Dungeon-Ende).
/// - Kartenpacks: 5 Stufen (CardPackCatalog), jede würfelt beim Öffnen
///   PackCardCount Karten nach der Raritäts-Gewichtung der Stufe
///   (CardPackOpener) und zeigt sie in einem Popup (PackOpenLayer).
/// - Charakter-Packs (Issue #6): 5 Stufen (CharacterPackCatalog), jede
///   liefert genau einen zufälligen Archetyp (CharacterPackOpener) ins
///   Roster (PlayerCharacterCollection.AddCharacter) - auch Duplikate
///   einer bereits besessenen Klasse sind erlaubt. Der neue Charakter
///   erscheint danach sofort in der Gruppenzusammenstellung (PartyScreen).
///
/// Gekaufte Karten wandern immer in PlayerCardCollection.BenchCards -
/// gleiches Prinzip wie alle neuen Karten bisher (erst über den
/// Deck-Screen aktiv ins Deck holen).
/// </summary>
public partial class Shop : Control
{
    private PackedScene _cardViewScene = null!;
    private Label _goldLabel = null!;
    private VBoxContainer _contentBox = null!;
    private Control _packOpenLayer = null!;

    public override void _Ready()
    {
        _cardViewScene = GD.Load<PackedScene>("res://scenes/CardView.tscn");
        _goldLabel = GetNode<Label>("MarginContainer/VBoxContainer/GoldLabel");
        _contentBox = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/ContentScroll/ContentBox");
        _packOpenLayer = GetNode<Control>("PackOpenLayer");

        var backButton = GetNode<Button>("MarginContainer/VBoxContainer/BackButton");
        backButton.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");

        ShopState.EnsureSpecialOffersRolled();

        UpdateGoldLabel();
        BuildContent();
    }

    private void UpdateGoldLabel()
    {
        _goldLabel.Text = $"Gold: {PlayerWallet.Gold}";
    }

    /// <summary>Baut den kompletten Inhaltsbereich neu auf - einfacher als einzelne Buttons/Preise nachträglich zu patchen, gleiches "alles neu rendern"-Prinzip wie DeckScreen.RenderAll.</summary>
    private void BuildContent()
    {
        foreach (Node child in _contentBox.GetChildren())
        {
            child.QueueFree();
        }

        _contentBox.AddChild(BuildSpecialOffersSection());
        _contentBox.AddChild(BuildCardPacksSection());
        _contentBox.AddChild(BuildCharacterPacksSection());
    }

    private Control BuildSpecialOffersSection()
    {
        var section = new VBoxContainer();
        section.AddThemeConstantOverride("separation", 8);
        section.AddChild(new Label { Text = "Sonderangebote", ThemeTypeVariation = "ColumnHeaderLabel" });

        if (ShopState.SpecialOffers.Count == 0)
        {
            section.AddChild(new Label
            {
                Text = "Keine Sonderangebote mehr - neue nach dem nächsten Dungeon.",
                ThemeTypeVariation = "CardDescriptionLabel",
            });
            return section;
        }

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 16);

        foreach (var card in ShopState.SpecialOffers)
        {
            int price = CardPackCatalog.SpecialOfferPriceFor(card.Rarity);

            var column = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
            column.AddThemeConstantOverride("separation", 6);

            var cardView = _cardViewScene.Instantiate<CardView>();
            column.AddChild(cardView);
            cardView.Populate(card);

            var buyButton = new Button
            {
                Text = $"Kaufen ({price} Gold)",
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                Disabled = PlayerWallet.Gold < price,
            };
            buyButton.Pressed += () => OnBuySpecialOffer(card, price);
            column.AddChild(buyButton);

            row.AddChild(column);
        }

        section.AddChild(row);
        return section;
    }

    private void OnBuySpecialOffer(CardDefinition card, int price)
    {
        if (PlayerWallet.Gold < price)
        {
            return;
        }

        PlayerWallet.Gold -= price;
        PlayerCardCollection.BenchCards.Add(card);
        ShopState.SpecialOffers.Remove(card);

        UpdateGoldLabel();
        BuildContent();
    }

    private Control BuildCardPacksSection()
    {
        var section = new VBoxContainer();
        section.AddThemeConstantOverride("separation", 8);
        section.AddChild(new Label { Text = "Kartenpacks", ThemeTypeVariation = "ColumnHeaderLabel" });

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 16);

        foreach (var tier in CardPackCatalog.AllTiers)
        {
            var panel = new PanelContainer { ThemeTypeVariation = "CardPanel" };
            var column = new VBoxContainer { CustomMinimumSize = new Vector2(160, 0) };
            column.AddThemeConstantOverride("separation", 6);

            column.AddChild(new Label
            {
                Text = tier.DisplayName,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            column.AddChild(new Label
            {
                Text = DescribeOdds(tier),
                ThemeTypeVariation = "CardDescriptionLabel",
                HorizontalAlignment = HorizontalAlignment.Center,
            });

            var buyButton = new Button
            {
                Text = $"Öffnen ({tier.Price} Gold)",
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                Disabled = PlayerWallet.Gold < tier.Price,
            };
            buyButton.Pressed += () => OnBuyPack(tier);
            column.AddChild(buyButton);

            panel.AddChild(column);
            row.AddChild(panel);
        }

        section.AddChild(row);
        return section;
    }

    private static string DescribeOdds(CardPackTier tier) =>
        string.Join("\n", tier.RarityWeights.Select(entry => $"{entry.Key}: {entry.Value}%"));

    private void OnBuyPack(CardPackTier tier)
    {
        if (PlayerWallet.Gold < tier.Price)
        {
            return;
        }

        PlayerWallet.Gold -= tier.Price;
        var drawnCards = CardPackOpener.Open(tier);
        foreach (var card in drawnCards)
        {
            PlayerCardCollection.BenchCards.Add(card);
        }

        UpdateGoldLabel();
        BuildContent();
        ShowPackOpenPopup(drawnCards);
    }

    private void ShowPackOpenPopup(List<CardDefinition> cards)
    {
        foreach (Node child in _packOpenLayer.GetChildren())
        {
            child.QueueFree();
        }

        var center = new CenterContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand,
            SizeFlagsVertical = Control.SizeFlags.Fill | Control.SizeFlags.Expand,
        };

        var panel = new PanelContainer { ThemeTypeVariation = "CardPanel" };
        var vbox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        vbox.AddThemeConstantOverride("separation", 16);

        vbox.AddChild(new Label
        {
            Text = "Du hast erhalten:",
            ThemeTypeVariation = "PopupHeaderLabel",
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        var cardsRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        cardsRow.AddThemeConstantOverride("separation", 16);
        foreach (var card in cards)
        {
            var cardView = _cardViewScene.Instantiate<CardView>();
            cardsRow.AddChild(cardView);
            cardView.Populate(card);
        }

        vbox.AddChild(cardsRow);

        var continueButton = new Button
        {
            Text = "Weiter",
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        vbox.AddChild(continueButton);

        panel.AddChild(vbox);
        center.AddChild(panel);
        _packOpenLayer.AddChild(center);
        _packOpenLayer.Visible = true;

        continueButton.Pressed += () =>
        {
            _packOpenLayer.Visible = false;
            center.QueueFree();
        };
    }

    /// <summary>Charakter-Packs (Issue #6): gleicher Aufbau wie BuildCardPacksSection, aber jede Stufe liefert genau einen zufälligen Archetyp statt mehrerer Karten.</summary>
    private Control BuildCharacterPacksSection()
    {
        var section = new VBoxContainer();
        section.AddThemeConstantOverride("separation", 8);
        section.AddChild(new Label { Text = "Charakter-Packs", ThemeTypeVariation = "ColumnHeaderLabel" });

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 16);

        foreach (var tier in CharacterPackCatalog.AllTiers)
        {
            var panel = new PanelContainer { ThemeTypeVariation = "CardPanel" };
            var column = new VBoxContainer { CustomMinimumSize = new Vector2(160, 0) };
            column.AddThemeConstantOverride("separation", 6);

            column.AddChild(new Label
            {
                Text = tier.DisplayName,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            column.AddChild(new Label
            {
                Text = "1 zufälliger Charakter",
                ThemeTypeVariation = "CardDescriptionLabel",
                HorizontalAlignment = HorizontalAlignment.Center,
            });

            var buyButton = new Button
            {
                Text = $"Öffnen ({tier.Price} Gold)",
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                Disabled = PlayerWallet.Gold < tier.Price,
            };
            buyButton.Pressed += () => OnBuyCharacterPack(tier);
            column.AddChild(buyButton);

            panel.AddChild(column);
            row.AddChild(panel);
        }

        section.AddChild(row);
        return section;
    }

    private void OnBuyCharacterPack(CharacterPackTier tier)
    {
        if (PlayerWallet.Gold < tier.Price)
        {
            return;
        }

        PlayerWallet.Gold -= tier.Price;
        var definition = CharacterPackOpener.Open(tier);
        PlayerCharacterCollection.AddCharacter(definition);

        UpdateGoldLabel();
        BuildContent();
        ShowCharacterPackOpenPopup(definition);
    }

    /// <summary>Gleiches Popup-Prinzip wie ShowPackOpenPopup, nur mit einer kleinen Charakter-Vorschau (Name/Klasse/HP/RK) statt einer CardView, da ein Charakter keine Karte ist.</summary>
    private void ShowCharacterPackOpenPopup(CharacterClassDefinition definition)
    {
        foreach (Node child in _packOpenLayer.GetChildren())
        {
            child.QueueFree();
        }

        var center = new CenterContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand,
            SizeFlagsVertical = Control.SizeFlags.Fill | Control.SizeFlags.Expand,
        };

        var panel = new PanelContainer { ThemeTypeVariation = "CardPanel" };
        var vbox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        vbox.AddThemeConstantOverride("separation", 12);

        vbox.AddChild(new Label
        {
            Text = "Neuer Abenteurer:",
            ThemeTypeVariation = "PopupHeaderLabel",
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        var characterPanel = new PanelContainer { ThemeTypeVariation = "CardPanel", CustomMinimumSize = new Vector2(180, 0) };
        var characterBox = new VBoxContainer();
        characterBox.AddThemeConstantOverride("separation", 4);
        characterBox.AddChild(new Label { Text = definition.Name, HorizontalAlignment = HorizontalAlignment.Center });
        characterBox.AddChild(new Label { Text = definition.ClassName, HorizontalAlignment = HorizontalAlignment.Center });
        characterBox.AddChild(new Label { Text = $"HP {definition.MaxHp} · RK {definition.BaseArmorClass}", HorizontalAlignment = HorizontalAlignment.Center });
        characterPanel.AddChild(characterBox);
        vbox.AddChild(characterPanel);

        vbox.AddChild(new Label
        {
            Text = "In der Gruppenzusammenstellung ('Gruppe managen') wählbar.",
            ThemeTypeVariation = "CardDescriptionLabel",
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        var continueButton = new Button
        {
            Text = "Weiter",
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        vbox.AddChild(continueButton);

        panel.AddChild(vbox);
        center.AddChild(panel);
        _packOpenLayer.AddChild(center);
        _packOpenLayer.Visible = true;

        continueButton.Pressed += () =>
        {
            _packOpenLayer.Visible = false;
            center.QueueFree();
        };
    }
}
