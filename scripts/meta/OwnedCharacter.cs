namespace PPRogueLite.Meta;

using PPRogueLite.Character;

/// <summary>
/// Ein vom Spieler besessener Charakter (Issue #5): wrappt eine
/// CharacterClassDefinition um den Lebendig-Status, der über Dungeon-Runs
/// hinweg bestehen bleibt (Permadeath für Gefährten, Issue #7 - der
/// gesteuerte Hauptcharakter/Leader ist davon nicht betroffen, siehe
/// PlayerCharacterCollection.MarkDead).
/// </summary>
public sealed class OwnedCharacter
{
    public OwnedCharacter(CharacterClassDefinition classDefinition)
    {
        ClassDefinition = classDefinition;
    }

    public CharacterClassDefinition ClassDefinition { get; }

    public bool IsAlive { get; set; } = true;
}
