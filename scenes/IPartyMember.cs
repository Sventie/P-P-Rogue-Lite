namespace PPRogueLite;

using Godot;

/// <summary>
/// Gemeinsame Schnittstelle für Gruppenmitglieder (Issue #5), implementiert
/// von Player (Hauptcharakter) und Companion (Gefährte). Enemy/Projectile
/// zielen darüber dynamisch auf das jeweils nächstgelegene lebende
/// Gruppenmitglied statt fest auf eine einmalig gecachte Player-Referenz -
/// nötig, seit mehrere Charaktere gleichzeitig im Spielfeld stehen können.
/// Position kommt bei beiden Implementierungen automatisch von Node2D.
///
/// Heal/ApplyHaste sind seit Issue #26 Teil des Interfaces, da CharacterLoadout
/// (Ausrüst-/Auslöse-Logik für Player UND Companion) Karteneffekte wie
/// "Atem holen" (Selbstheilung) oder "Adrenalin" (Speed-Boost nach Krit)
/// unabhängig davon auslösen kann, welcher Charakter die Karte trägt.
/// </summary>
public interface IPartyMember
{
    Vector2 Position { get; }

    int EffectiveArmorClass { get; }

    bool IsDefeated { get; }

    void TakeDamage(int amount);

    void Heal(int amount);

    void ApplySlow(float duration, float multiplier);

    void ApplyHaste(float duration, float multiplier);
}

/// <summary>Sucht das nächstgelegene lebende Mitglied der "party"-Gruppe - gemeinsam genutzt von Enemy, Projectile und Companion, um Duplizierung der Distanz-Scan-Logik zu vermeiden.</summary>
public static class PartyGroupQuery
{
    public static IPartyMember? FindNearestPartyMember(this Node caller, Vector2 from)
    {
        IPartyMember? nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Node node in caller.GetTree().GetNodesInGroup("party"))
        {
            if (node is not IPartyMember member || member.IsDefeated)
            {
                continue;
            }

            float distance = from.DistanceTo(member.Position);
            if (distance < nearestDistance)
            {
                nearest = member;
                nearestDistance = distance;
            }
        }

        return nearest;
    }
}
