namespace PPRogueLite.Meta;

using System;
using PPRogueLite.Character;

/// <summary>
/// Ein vom Spieler besessener Charakter (Issue #5): wrappt eine
/// CharacterClassDefinition um den Lebendig-Status, der über Dungeon-Runs
/// hinweg bestehen bleibt (Permadeath für Gefährten, Issue #7 - der
/// gesteuerte Hauptcharakter/Leader ist davon nicht betroffen, siehe
/// PlayerCharacterCollection.MarkDead).
///
/// Seit dem Charakter-Shop (Issue #6) kann es mehrere OwnedCharacter
/// derselben Klasse geben (z. B. zwei Bogenschützen) - Id ist deshalb der
/// stabile Schlüssel für Instanz-bezogenen Zustand (aktuell nur
/// DungeonRun.SavedCompanionHp), ClassDefinition allein reicht dafür nicht
/// mehr aus.
/// </summary>
public sealed class OwnedCharacter
{
    public OwnedCharacter(CharacterClassDefinition classDefinition)
    {
        Id = Guid.NewGuid();
        ClassDefinition = classDefinition;
    }

    public Guid Id { get; }

    public CharacterClassDefinition ClassDefinition { get; }

    public bool IsAlive { get; set; } = true;
}
