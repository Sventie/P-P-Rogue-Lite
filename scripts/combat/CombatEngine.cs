namespace PPRogueLite.Combat;

using System;
using PPRogueLite.Character;

/// <summary>
/// Resolves W20-Angriffswürfe und Werte-Checks für einen Kampf zwischen
/// PlayerCharacter und Enemy. Kennt keine UI - Main.cs meldet sich über die
/// log/updateReadout-Callbacks zurück, um Log und Anzeige zu aktualisieren.
/// </summary>
public sealed class CombatEngine
{
    private readonly Action<string, LogTag> _log;
    private readonly Action<string> _updateReadout;

    public CombatEngine(PlayerCharacter player, Enemy foe, Action<string, LogTag> log, Action<string> updateReadout)
    {
        Player = player;
        Foe = foe;
        _log = log;
        _updateReadout = updateReadout;
    }

    public PlayerCharacter Player { get; }

    public Enemy Foe { get; }

    public void Log(string message, LogTag tag = LogTag.System) => _log(message, tag);

    public static string FormatMod(int modifier) => (modifier >= 0 ? "+" : string.Empty) + modifier;

    public AttackResult AttackRoll(int modifier, string label)
    {
        int roll;
        string detail;

        if (Player.HasAdvantageNextAttack)
        {
            int first = Dice.Roll(20);
            int second = Dice.Roll(20);
            roll = Math.Max(first, second);
            detail = $"W20 Vorteil: {first} / {second} -> {roll}";
            Player.HasAdvantageNextAttack = false;
        }
        else
        {
            roll = Dice.Roll(20);
            detail = $"W20: {roll}";
        }

        int total = roll + modifier;
        bool hit = total >= Foe.ArmorClass;

        Log($"{label}: {detail} {FormatMod(modifier)} = {total} gegen RK {Foe.ArmorClass}", LogTag.System);
        _updateReadout($"{label}: Wurf {total} gegen Rüstungsklasse {Foe.ArmorClass} — {(hit ? "Treffer" : "Verfehlt")}");

        return new AttackResult { Roll = roll, Total = total, IsHit = hit };
    }

    public AbilityCheckResult AbilityCheck(int modifier, int dc, string label)
    {
        int roll = Dice.Roll(20);
        int total = roll + modifier;
        bool success = total >= dc;

        Log($"{label}: Wurf {roll} {FormatMod(modifier)} = {total} gegen SG {dc}", LogTag.System);

        return new AbilityCheckResult { Roll = roll, Total = total, Success = success };
    }

    public AttackResult EnemyAttack()
    {
        int roll = Dice.Roll(20);
        int total = roll + Foe.AttackBonus;
        int playerAc = Player.ArmorClass;
        bool hit = total >= playerAc;

        Log($"{Foe.Name} greift an: W20 {roll} {FormatMod(Foe.AttackBonus)} = {total} gegen deine RK {playerAc}", LogTag.System);
        _updateReadout($"{Foe.Name}-Angriff: {total} gegen deine Rüstungsklasse {playerAc} — {(hit ? "Treffer" : "Verfehlt")}");

        if (hit)
        {
            int damageRoll = Dice.Roll(Foe.DamageDie);
            int damage = damageRoll + Foe.DamageBonus;
            Player.TakeDamage(damage);
            Log($"Du wirst getroffen! Schaden: {damageRoll} (W{Foe.DamageDie}) +{Foe.DamageBonus} = {damage}", LogTag.Bad);
        }
        else
        {
            Log("Der Angriff geht daneben.", LogTag.Good);
        }

        Player.TempArmorClass = 0;

        return new AttackResult { Roll = roll, Total = total, IsHit = hit };
    }
}
