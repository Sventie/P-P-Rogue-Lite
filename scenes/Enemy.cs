namespace PPRogueLite;

using Godot;
using PPRogueLite.Combat;
using PPRogueLite.Enemies;
using CharacterEnemy = PPRogueLite.Character.Enemy;

/// <summary>
/// Gemeinsamer Gegner-Node der Echtzeit-Arena (Issue #9, löst das bisher
/// goblin-spezifische EnemyGoblin.cs ab): Bewegung/Angriff werden nicht
/// pro Gegnertyp dupliziert, sondern über eine EnemyDefinition
/// (PPRogueLite.Enemies, kein Godot-Bezug) datengetrieben verzweigt -
/// analog zu Player.TriggerAbility, das über CardDefinition.Id switcht.
///
/// Arena.SpawnEnemy() instanziert diese Szene, ruft AddChild() und danach
/// Initialize(definition) auf (Godot-Lifecycle-Regel: _Ready() feuert
/// erst NACH AddChild(), Initialize() setzt die eigentlichen Werte also
/// erst danach - siehe CLAUDE.md "Node-Lifecycle").
///
/// Kein Godot-Physik-Kollisionssystem - Distanzprüfung per Hand, um
/// Kollisions-Layer/Masken-Konfiguration (ungetestet) zu vermeiden.
/// </summary>
public partial class Enemy : Node2D
{
    private const float HpBarWidth = 30f;
    private const float HpBarHeight = 4f;
    private const float RangedTolerance = 40f;
    private const float RetreatDuration = 1.0f;
    private const float SlowDuration = 2f;
    private const float SlowMultiplier = 0.5f;
    private const float SummonScatterRadius = 60f;
    private const float PoisonTickInterval = 1f; // Issue #14: Sekunden zwischen zwei Gift-Ticks
    private const float BurnTickInterval = 1f; // Issue #23
    private const float BleedTickInterval = 1f; // Issue #24

    private static readonly Color MissColor = new(0.662745f, 0.603922f, 0.470588f);
    private static readonly Color SlowColor = new(0.45f, 0.25f, 0.55f);
    private static readonly Color HpBarBackground = new(0f, 0f, 0f, 0.5f);
    private static readonly Color HpBarFill = new(0.352941f, 0.478431f, 0.309804f);
    private static readonly Color SlamTelegraphColor = new(0.611765f, 0.15f, 0.15f, 0.35f);
    private static readonly Color PoisonColor = new(0.3f, 0.75f, 0.25f);
    private static readonly Color BurnColor = new(0.85f, 0.45f, 0.15f);
    private static readonly Color BleedColor = new(0.55f, 0.1f, 0.15f);

    private enum SlamState
    {
        Idle,
        Telegraphing,
    }

    public CharacterEnemy Stats { get; private set; } = null!;

    private EnemyDefinition _definition = null!;
    private BossDefinition? _bossDefinition;
    private Player? _player;
    private Arena? _arena;
    private float _attackTimer;
    private float _retreatTimer;
    private float _summonTimer;
    private float _slamTimer;
    private SlamState _slamState = SlamState.Idle;
    private float _slamTelegraphTimer;
    private Vector2 _slamDirection = Vector2.Right;
    private float _poisonTimer;
    private float _poisonTickTimer;
    private int _poisonDamagePerTick;
    private float _burnTimer;
    private float _burnTickTimer;
    private int _burnDamagePerTick;
    private int _burnStacks;
    private float _bleedTimer;
    private float _bleedTickTimer;
    private float _bleedPercentPerTick;
    private bool _disabled;
    private PackedScene _floatingTextScene = null!;

    public override void _Ready()
    {
        AddToGroup("enemies");
        _floatingTextScene = GD.Load<PackedScene>("res://scenes/FloatingText.tscn");

        var players = GetTree().GetNodesInGroup("player");
        _player = players.Count > 0 ? players[0] as Player : null;
        _arena = GetParent() as Arena;
    }

    /// <summary>Setzt Stats/Verhalten des Gegners - erst nach AddChild() aufrufen (siehe Klassenkommentar).</summary>
    public void Initialize(EnemyDefinition definition)
    {
        _definition = definition;
        _bossDefinition = definition as BossDefinition;
        _summonTimer = _bossDefinition?.SummonCooldown ?? 0f;
        _slamTimer = _bossDefinition?.SlamCooldown ?? 0f;
        Stats = new CharacterEnemy
        {
            Name = definition.DisplayName,
            MaxHp = definition.MaxHp,
            Hp = definition.MaxHp,
            ArmorClass = definition.ArmorClass,
            AttackBonus = definition.AttackBonus,
            DamageDie = definition.DamageDie,
            DamageBonus = definition.DamageBonus,
        };
        QueueRedraw();
    }

    public override void _Draw()
    {
        float radius = _definition.Radius;
        DrawCircle(Vector2.Zero, radius, ColorFor(_definition.Id));

        // Bei größeren Gegnern (aktuell nur der Boss) skaliert der HP-Balken
        // mit dem Radius mit, statt bei fester Breite kaum sichtbar zu wirken.
        float barWidth = Mathf.Max(HpBarWidth, radius * 1.4f);
        float hpBarOffsetY = -(radius + 10f);
        var topLeft = new Vector2(-barWidth / 2f, hpBarOffsetY);
        DrawRect(new Rect2(topLeft, new Vector2(barWidth, HpBarHeight)), HpBarBackground);

        float ratio = Stats.MaxHp > 0 ? Mathf.Clamp((float)Stats.Hp / Stats.MaxHp, 0f, 1f) : 0f;
        DrawRect(new Rect2(topLeft, new Vector2(barWidth * ratio, HpBarHeight)), HpBarFill);

        if (_slamState == SlamState.Telegraphing && _bossDefinition is not null)
        {
            var perpendicular = new Vector2(-_slamDirection.Y, _slamDirection.X);
            var points = new[]
            {
                perpendicular * _bossDefinition.SlamHalfWidth,
                (perpendicular * _bossDefinition.SlamHalfWidth) + (_slamDirection * _bossDefinition.SlamRange),
                (-perpendicular * _bossDefinition.SlamHalfWidth) + (_slamDirection * _bossDefinition.SlamRange),
                -perpendicular * _bossDefinition.SlamHalfWidth,
            };
            DrawColoredPolygon(points, SlamTelegraphColor);
        }

        // Konzentrische Ringe mit steigendem Versatz, damit mehrere
        // gleichzeitig aktive Status-Effekte alle sichtbar bleiben, statt
        // sich zu überlappen.
        if (_poisonTimer > 0f)
        {
            DrawArc(Vector2.Zero, radius + 4f, 0f, Mathf.Tau, 24, PoisonColor, 3f, true);
        }

        if (_burnTimer > 0f)
        {
            DrawArc(Vector2.Zero, radius + 8f, 0f, Mathf.Tau, 24, BurnColor, 3f, true);
        }

        if (_bleedTimer > 0f)
        {
            DrawArc(Vector2.Zero, radius + 12f, 0f, Mathf.Tau, 24, BleedColor, 3f, true);
        }
    }

    public override void _Process(double delta)
    {
        if (_disabled || _player is null || Stats.IsDefeated)
        {
            return;
        }

        float dt = (float)delta;
        UpdateStatusEffects(dt);
        if (Stats.IsDefeated)
        {
            return;
        }

        switch (_definition.Movement)
        {
            case EnemyMovement.Melee:
                UpdateMelee(dt);
                break;
            case EnemyMovement.Ranged:
                UpdateRanged(dt);
                break;
            case EnemyMovement.HitAndRun:
                UpdateHitAndRun(dt);
                break;
            case EnemyMovement.Boss:
                UpdateBoss(dt);
                break;
        }
    }

    private void UpdateMelee(float delta)
    {
        Vector2 toPlayer = _player!.Position - Position;
        float distance = toPlayer.Length();

        if (distance > _definition.EngagementRange)
        {
            Position += toPlayer.Normalized() * _definition.MoveSpeed * delta;
            return;
        }

        _attackTimer -= delta;
        if (_attackTimer <= 0f)
        {
            _attackTimer = _definition.AttackCooldown;
            AttackPlayer();
        }
    }

    /// <summary>Hält EngagementRange als Wunschabstand (nähert sich/weicht zurück), feuert Projektile sobald in Reichweite.</summary>
    private void UpdateRanged(float delta)
    {
        Vector2 toPlayer = _player!.Position - Position;
        float distance = toPlayer.Length();

        if (distance > _definition.EngagementRange + RangedTolerance)
        {
            Position += toPlayer.Normalized() * _definition.MoveSpeed * delta;
        }
        else if (distance < _definition.EngagementRange - RangedTolerance)
        {
            Position -= toPlayer.Normalized() * _definition.MoveSpeed * delta;
            ClampToViewport();
        }

        _attackTimer -= delta;
        if (_attackTimer <= 0f && distance <= _definition.EngagementRange + RangedTolerance)
        {
            _attackTimer = _definition.AttackCooldown;
            FireProjectile(toPlayer.Normalized());
        }
    }

    /// <summary>Nähert sich, greift bei Kontakt an, zieht sich danach für RetreatDuration zurück statt stehen zu bleiben.</summary>
    private void UpdateHitAndRun(float delta)
    {
        Vector2 toPlayer = _player!.Position - Position;

        if (_retreatTimer > 0f)
        {
            _retreatTimer -= delta;
            Position -= toPlayer.Normalized() * _definition.MoveSpeed * delta;
            ClampToViewport();
            return;
        }

        float distance = toPlayer.Length();
        if (distance > _definition.EngagementRange)
        {
            Position += toPlayer.Normalized() * _definition.MoveSpeed * delta;
            return;
        }

        _attackTimer -= delta;
        if (_attackTimer <= 0f)
        {
            _attackTimer = _definition.AttackCooldown;
            AttackPlayer();
            _retreatTimer = RetreatDuration;
        }
    }

    /// <summary>
    /// Bossverhalten (Issue #11): normaler Kontaktangriff wie Melee, dazu
    /// zwei unabhängige Cooldowns für Verstärkung-rufen und den
    /// telegraphierten Keulenschlag. Während Wind-up/Auflösung des
    /// Keulenschlags steht der Boss still (kein Movement/Kontaktangriff),
    /// damit der angezeigte Hitbox-Bereich verlässlich stimmt.
    /// </summary>
    private void UpdateBoss(float delta)
    {
        var boss = _bossDefinition!;

        UpdateSummon(boss, delta);

        if (_slamState == SlamState.Telegraphing)
        {
            _slamTelegraphTimer -= delta;
            if (_slamTelegraphTimer <= 0f)
            {
                ExecuteSlam(boss);
            }

            return;
        }

        _slamTimer -= delta;
        if (_slamTimer <= 0f)
        {
            StartSlamTelegraph(boss);
            return;
        }

        UpdateMelee(delta);
    }

    private void UpdateSummon(BossDefinition boss, float delta)
    {
        _summonTimer -= delta;
        if (_summonTimer > 0f)
        {
            return;
        }

        _summonTimer = boss.SummonCooldown;
        for (int i = 0; i < boss.SummonCount; i++)
        {
            _arena?.SpawnEnemyNear(boss.SummonedEnemy, Position, SummonScatterRadius);
        }

        SpawnFloatingText(ColorFor(_definition.Id), "Verstärkung!");
    }

    /// <summary>Legt die Angriffsrichtung EINMALIG beim Start des Telegraphs fest (nicht laufend nachgeführt), damit die angezeigte Hitbox verlässlich ist.</summary>
    private void StartSlamTelegraph(BossDefinition boss)
    {
        if (_player is null)
        {
            return;
        }

        _slamDirection = (_player.Position - Position).Normalized();
        if (_slamDirection == Vector2.Zero)
        {
            _slamDirection = Vector2.Right;
        }

        _slamState = SlamState.Telegraphing;
        _slamTelegraphTimer = boss.SlamTelegraphDuration;
        QueueRedraw();
    }

    /// <summary>Löst den Keulenschlag auf: nur wer noch in der (vorher sichtbaren) Hitbox steht, kann getroffen werden - Ausweichen per Wegbewegen ist somit garantiert wirksam.</summary>
    private void ExecuteSlam(BossDefinition boss)
    {
        _slamState = SlamState.Idle;
        _slamTimer = boss.SlamCooldown;
        QueueRedraw();

        if (_player is null || !IsInSlamHitbox(boss, _player.Position))
        {
            return;
        }

        int roll = Dice.Roll(20);
        int total = roll + boss.SlamAttackBonus;
        if (total < _player.EffectiveArmorClass)
        {
            SpawnFloatingText(MissColor, "Verfehlt");
            return;
        }

        int damage = Dice.Roll(boss.SlamDamageDie) + boss.SlamDamageBonus;
        _player.TakeDamage(damage);
        SpawnFloatingText(ColorFor(_definition.Id), damage.ToString());
    }

    private bool IsInSlamHitbox(BossDefinition boss, Vector2 worldPoint)
    {
        Vector2 relative = worldPoint - Position;
        var perpendicular = new Vector2(-_slamDirection.Y, _slamDirection.X);

        float forward = relative.Dot(_slamDirection);
        float side = Mathf.Abs(relative.Dot(perpendicular));

        return forward >= 0f && forward <= boss.SlamRange && side <= boss.SlamHalfWidth;
    }

    /// <summary>
    /// Hält Rückzugsbewegung (Ranged weicht zurück, HitAndRun zieht sich
    /// zurück) innerhalb des Fensters - der Rand ist eine Wand. Nahkampf-
    /// Bewegung braucht das nicht: sie läuft immer auf den (selbst schon
    /// geklemmten) Spieler zu, kann das Fenster also nie verlassen.
    /// </summary>
    private void ClampToViewport()
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;
        float radius = _definition.Radius;
        Position = new Vector2(
            Mathf.Clamp(Position.X, radius, viewportSize.X - radius),
            Mathf.Clamp(Position.Y, radius, viewportSize.Y - radius));
    }

    private void AttackPlayer()
    {
        if (_player is null)
        {
            return;
        }

        int roll = Dice.Roll(20);
        int total = roll + Stats.AttackBonus;
        bool hit = total >= _player.EffectiveArmorClass;

        if (!hit)
        {
            SpawnFloatingText(MissColor, "Verfehlt");
            return;
        }

        int damage = Dice.Roll(Stats.DamageDie) + Stats.DamageBonus;
        _player.TakeDamage(damage);
        SpawnFloatingText(ColorFor(_definition.Id), damage.ToString());
        ApplyOnHitEffect();
    }

    private void FireProjectile(Vector2 direction)
    {
        var projectile = new Projectile
        {
            Direction = direction,
            AttackBonus = Stats.AttackBonus,
            DamageDie = Stats.DamageDie,
            DamageBonus = Stats.DamageBonus,
            OnHitEffect = _definition.OnHitEffect,
            Color = ColorFor(_definition.Id),
        };
        GetParent()?.AddChild(projectile);
        projectile.GlobalPosition = GlobalPosition;
    }

    private void ApplyOnHitEffect()
    {
        if (_definition.OnHitEffect == EnemyOnHitEffect.Slow)
        {
            _player?.ApplySlow(SlowDuration, SlowMultiplier);
            SpawnFloatingText(SlowColor, "Verlangsamt!");
        }
    }

    /// <summary>Tickt alle drei Damage-over-Time-Effekte (Gift/Brand/Blutung, Issues #14/#23/#24). Bricht früh ab, sobald der Gegner dabei besiegt wird, damit kein weiterer Tick/die Bewegung noch auf einen bereits besiegten Gegner zugreift.</summary>
    private void UpdateStatusEffects(float delta)
    {
        UpdatePoison(delta);
        if (Stats.IsDefeated)
        {
            return;
        }

        UpdateBurn(delta);
        if (Stats.IsDefeated)
        {
            return;
        }

        UpdateBleed(delta);
    }

    /// <summary>
    /// Wendet einen Gift-Effekt an bzw. erneuert einen bestehenden (Issue
    /// #14, ausgelöst z. B. über die Giftklinge-Modifikatorkarte): pro
    /// PoisonTickInterval automatischer Schaden über duration Sekunden.
    /// Kein Stacking - eine erneute Anwendung überschreibt Dauer und
    /// Schaden pro Tick, gleiches Muster wie Player.ApplySlow/ApplyHaste.
    /// </summary>
    public void ApplyPoison(int damagePerTick, float duration)
    {
        bool wasPoisoned = _poisonTimer > 0f;
        _poisonDamagePerTick = damagePerTick;
        _poisonTimer = duration;
        _poisonTickTimer = PoisonTickInterval;

        if (!wasPoisoned)
        {
            QueueRedraw();
        }
    }

    private void UpdatePoison(float delta)
    {
        if (_poisonTimer <= 0f)
        {
            return;
        }

        _poisonTimer -= delta;
        _poisonTickTimer -= delta;

        if (_poisonTickTimer <= 0f)
        {
            _poisonTickTimer = PoisonTickInterval;
            TakeDamage(_poisonDamagePerTick);
            SpawnFloatingText(PoisonColor, _poisonDamagePerTick.ToString());
        }

        if (_poisonTimer <= 0f)
        {
            _poisonTimer = 0f;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Wendet Brand an bzw. erhöht die Stacks eines bestehenden Brands
    /// (Issue #23, ausgelöst über die Brand-Modifikatorkarte): im
    /// Gegensatz zu Gift erhöht eine erneute Anwendung den Schaden pro Tick
    /// (bis maxStacks), statt nur die Dauer zu erneuern - belohnt
    /// wiederholte Treffer statt nur den ersten.
    /// </summary>
    public void ApplyBurn(int damagePerStackPerTick, float duration, int maxStacks)
    {
        bool wasBurning = _burnTimer > 0f;
        _burnStacks = Mathf.Min(_burnStacks + 1, maxStacks);
        _burnDamagePerTick = damagePerStackPerTick * _burnStacks;
        _burnTimer = duration;
        _burnTickTimer = BurnTickInterval;

        if (!wasBurning)
        {
            QueueRedraw();
        }
    }

    private void UpdateBurn(float delta)
    {
        if (_burnTimer <= 0f)
        {
            return;
        }

        _burnTimer -= delta;
        _burnTickTimer -= delta;

        if (_burnTickTimer <= 0f)
        {
            _burnTickTimer = BurnTickInterval;
            TakeDamage(_burnDamagePerTick);
            SpawnFloatingText(BurnColor, _burnDamagePerTick.ToString());
        }

        if (_burnTimer <= 0f)
        {
            _burnTimer = 0f;
            _burnStacks = 0;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Wendet Blutung an bzw. erneuert sie (Issue #24, ausgelöst über die
    /// Blutung-Modifikatorkarte): Schaden pro Tick ist ein Prozentsatz der
    /// maximalen HP des Ziels statt eines festen Werts - macht Blutung zum
    /// Konter gegen tanky Ziele (Höhlentroll, Oger-Häuptling). Kein
    /// Stacking, wie Gift - eine erneute Anwendung erneuert nur die Dauer.
    /// </summary>
    public void ApplyBleed(float percentOfMaxHpPerTick, float duration)
    {
        bool wasBleeding = _bleedTimer > 0f;
        _bleedPercentPerTick = percentOfMaxHpPerTick;
        _bleedTimer = duration;
        _bleedTickTimer = BleedTickInterval;

        if (!wasBleeding)
        {
            QueueRedraw();
        }
    }

    private void UpdateBleed(float delta)
    {
        if (_bleedTimer <= 0f)
        {
            return;
        }

        _bleedTimer -= delta;
        _bleedTickTimer -= delta;

        if (_bleedTickTimer <= 0f)
        {
            _bleedTickTimer = BleedTickInterval;
            int damage = Mathf.Max(1, Mathf.RoundToInt(Stats.MaxHp * _bleedPercentPerTick));
            TakeDamage(damage);
            SpawnFloatingText(BleedColor, damage.ToString());
        }

        if (_bleedTimer <= 0f)
        {
            _bleedTimer = 0f;
            QueueRedraw();
        }
    }

    public void TakeDamage(int amount)
    {
        Stats.TakeDamage(amount);
        QueueRedraw();

        if (!Stats.IsDefeated)
        {
            return;
        }

        _player?.GrantXp(1);
        QueueFree();
    }

    public void SetDisabled(bool disabled)
    {
        _disabled = disabled;
    }

    private void SpawnFloatingText(Color color, string text)
    {
        var floatingText = _floatingTextScene.Instantiate<FloatingText>();
        GetParent()?.AddChild(floatingText);
        floatingText.GlobalPosition = GlobalPosition;
        floatingText.ShowText(text, color);
    }

    private static Color ColorFor(string id) => id switch
    {
        "goblin" => new Color(0.611765f, 0.231373f, 0.231373f),
        "troll" => new Color(0.4f, 0.35f, 0.25f),
        "archer" => new Color(0.3f, 0.5f, 0.3f),
        "schamane" => new Color(0.45f, 0.25f, 0.55f),
        "schurke" => new Color(0.25f, 0.25f, 0.4f),
        "oger_haeuptling" => new Color(0.35f, 0.15f, 0.1f),
        _ => new Color(0.5f, 0.5f, 0.5f),
    };
}
