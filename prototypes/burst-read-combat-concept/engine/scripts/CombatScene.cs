using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// PROTOTYPE - NOT FOR PRODUCTION
// Question: 验证 Burst+Read 战斗的 4 个 feel 类问题
//   1. "一招分胜负" 视觉爆发感是否成立？
//   2. intent tell 的 UI 呈现是否清晰？
//   3. 回合切换 pacing 是否符合 "短而重"？
//   4. simultaneous resolution 的视觉清晰度？
// Date: 2026-06-02

public partial class CombatScene : Control
{
    // ============== Data ==============
    public enum Sys { Gang, Rou, Qiao }
    public enum Cat { Attack, Defense, Counter, Info }

    public class Move
    {
        public string Name = "";
        public Sys System;
        public Cat Category;
        public int Cost;
        public int Damage;
        public int DefenseValue;
        public bool StaggersOnHit;
        public Sys CountersSystem;
        public string Flavor = "";

        public string SystemName => System switch
        {
            Sys.Gang => "刚",
            Sys.Rou => "柔",
            Sys.Qiao => "巧",
            _ => "?"
        };

        public Color SystemColor => System switch
        {
            Sys.Gang => new Color("e85a4d"),
            Sys.Rou => new Color("5b9fdb"),
            Sys.Qiao => new Color("7ac765"),
            _ => Colors.Gray
        };
    }

    public class Combatant
    {
        public string Name = "";
        public int MaxHP;
        public int HP;
        public int NeiXi = 3;
        public int MaxNeiXi = 5;
        public int Stagger = 0;
        public int MaxStagger = 3;
        public bool StaggeredThisRound = false;
        public bool DecisiveUsed = false;
        public bool MustSkipNextRound = false;
        public bool TookDamageThisRound = false;
        public List<Move> Moves = new();
        public bool IsAlive => HP > 0;
        public bool FullHealth => HP == MaxHP;
    }

    // ============== State ==============
    private Combatant _player = null!;
    private Combatant _enemy = null!;
    private Move? _enemyIntent;
    private int _round = 0;
    private bool _gameOver = false;
    private TaskCompletionSource<(Move move, bool decisive)>? _playerChoice;
    private bool _reactionSucceededThisRound = false; // updated during Resolve
    private bool _reactionSucceededLastRound = false; // for decisive window condition

    // ============== UI ==============
    private Label _playerName = null!, _playerStats = null!;
    private Label _enemyName = null!, _enemyStats = null!, _enemyIntentLabel = null!;
    private Label _roundLabel = null!;
    private RichTextLabel _log = null!;
    private VBoxContainer _moveButtonsBox = null!;
    private Label _decisiveBanner = null!;
    private Label _decisiveOverlay = null!;
    private Button _restartButton = null!;
    private Control _damageLayer = null!;
    private Control _playerCard = null!;
    private Control _enemyCard = null!;

    public override void _Ready()
    {
        BuildUI();
        _ = StartNewCombat();
    }

    // ===================== UI BUILD =====================
    private void BuildUI()
    {
        // dark background
        var bg = new ColorRect { Color = new Color("1a1a1f") };
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // ---- Top: status row ----
        var topRow = new HBoxContainer { Position = new Vector2(40, 40), CustomMinimumSize = new Vector2(1200, 200) };
        AddChild(topRow);

        _playerCard = MakeCard("主角 (孤山弟子)", out _playerName, out _playerStats);
        topRow.AddChild(_playerCard);

        // center: round + intent
        var center = new VBoxContainer { CustomMinimumSize = new Vector2(360, 200) };
        topRow.AddChild(center);

        _roundLabel = new Label { Text = "回合 0", HorizontalAlignment = HorizontalAlignment.Center };
        _roundLabel.AddThemeFontSizeOverride("font_size", 24);
        center.AddChild(_roundLabel);

        var intentBox = new PanelContainer { CustomMinimumSize = new Vector2(360, 120) };
        var intentStyle = new StyleBoxFlat { BgColor = new Color("2a2a36"), BorderColor = new Color("44445a"), };
        intentStyle.SetBorderWidthAll(2);
        intentStyle.SetCornerRadiusAll(8);
        intentStyle.ContentMarginLeft = 12;
        intentStyle.ContentMarginRight = 12;
        intentStyle.ContentMarginTop = 12;
        intentStyle.ContentMarginBottom = 12;
        intentBox.AddThemeStyleboxOverride("panel", intentStyle);
        center.AddChild(intentBox);

        _enemyIntentLabel = new Label
        {
            Text = "—",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _enemyIntentLabel.AddThemeFontSizeOverride("font_size", 22);
        intentBox.AddChild(_enemyIntentLabel);

        _enemyCard = MakeCard("李无双 (叛逃者)", out _enemyName, out _enemyStats);
        topRow.AddChild(_enemyCard);

        // ---- Mid: log ----
        _log = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollFollowing = true,
            Position = new Vector2(40, 270),
            CustomMinimumSize = new Vector2(1200, 200),
            FitContent = true,
        };
        AddChild(_log);

        // ---- Bottom: move buttons ----
        var bottomBanner = new VBoxContainer { Position = new Vector2(40, 490), CustomMinimumSize = new Vector2(1200, 200) };
        AddChild(bottomBanner);

        _decisiveBanner = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _decisiveBanner.AddThemeFontSizeOverride("font_size", 18);
        bottomBanner.AddChild(_decisiveBanner);

        _moveButtonsBox = new VBoxContainer { CustomMinimumSize = new Vector2(1200, 170) };
        bottomBanner.AddChild(_moveButtonsBox);

        // ---- Decisive overlay (hidden) ----
        _decisiveOverlay = new Label
        {
            Text = "一招分胜负！",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Modulate = new Color(1, 1, 1, 0),
        };
        _decisiveOverlay.AddThemeFontSizeOverride("font_size", 96);
        _decisiveOverlay.AddThemeColorOverride("font_color", new Color("e8d040"));
        _decisiveOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_decisiveOverlay);

        // ---- Damage layer (popups) ----
        _damageLayer = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _damageLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_damageLayer);

        // ---- Restart button (hidden until game over) ----
        _restartButton = new Button
        {
            Text = "重新开始",
            Position = new Vector2(560, 640),
            CustomMinimumSize = new Vector2(160, 50),
            Visible = false,
        };
        _restartButton.Pressed += () => { _ = StartNewCombat(); };
        AddChild(_restartButton);
    }

    private Control MakeCard(string title, out Label nameLabel, out Label statsLabel)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(360, 200) };
        var style = new StyleBoxFlat { BgColor = new Color("262630"), BorderColor = new Color("44445a") };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
        style.ContentMarginLeft = 16; style.ContentMarginRight = 16;
        style.ContentMarginTop = 12; style.ContentMarginBottom = 12;
        panel.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        panel.AddChild(vbox);

        nameLabel = new Label { Text = title };
        nameLabel.AddThemeFontSizeOverride("font_size", 20);
        vbox.AddChild(nameLabel);

        var sep = new HSeparator();
        vbox.AddChild(sep);

        statsLabel = new Label { Text = "" };
        statsLabel.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(statsLabel);

        return panel;
    }

    // ===================== COMBAT FLOW =====================
    private async Task StartNewCombat()
    {
        _player = MakePlayer();
        _enemy = MakeEnemy();
        _round = 0;
        _gameOver = false;
        _enemyIntent = null;
        _restartButton.Visible = false;
        _log.Clear();
        _log.AppendText("[color=#888]竹林清晨。李无双独立于碎石间，腰间血剑未鞘。[/color]\n\n");
        UpdateUI();

        while (!_gameOver)
        {
            await RunRound();
        }
    }

    private async Task RunRound()
    {
        _round++;
        // roll round-flags forward
        _reactionSucceededLastRound = _reactionSucceededThisRound;
        _reactionSucceededThisRound = false;
        _player.StaggeredThisRound = false;
        _enemy.StaggeredThisRound = false;
        _player.TookDamageThisRound = false;
        _enemy.TookDamageThisRound = false;

        // resource recovery (except round 1)
        if (_round > 1)
        {
            _player.NeiXi = Math.Min(_player.MaxNeiXi, _player.NeiXi + 2); // 浩然心法 +2
            _enemy.NeiXi = Math.Min(_enemy.MaxNeiXi, _enemy.NeiXi + 1);
        }

        // skip if player staggered out
        if (_player.MustSkipNextRound)
        {
            _player.MustSkipNextRound = false;
            Log($"[color=#ff7878]主角破绽爆发，跳过本回合。[/color]");
            // enemy still acts
            _enemyIntent = PickEnemyMove();
            await Sleep(400);
            await FlashIntentReveal();
            await Sleep(600);
            await ApplyEnemySoloAttack();
            await EndOfRoundCleanup();
            return;
        }

        // Phase 1: enemy intent reveal
        _enemyIntent = PickEnemyMove();
        await Sleep(300);
        await FlashIntentReveal();
        Log($"[color=#cccccc]──── 回合 {_round} ────[/color]");
        Log($"李无双 意图: [color={EnemyIntentColorHex()}]{_enemyIntent.SystemName}系 → 主角[/color]");

        // Phase 2: wait for player choice
        UpdateUI();
        var (playerMove, decisive) = await WaitForPlayerChoice();

        // Phase 3: resolution
        await Resolve(playerMove, _enemyIntent, decisive);

        // Phase 4: end of round
        await EndOfRoundCleanup();
    }

    private async Task EndOfRoundCleanup()
    {
        // stagger decay rule (v0.2 fix #3)
        if (!_player.StaggeredThisRound && _player.Stagger > 0)
        {
            _player.Stagger--;
            Log($"[color=#888]主角调息，破绽 -1（→{_player.Stagger}）[/color]");
        }
        if (!_enemy.StaggeredThisRound && _enemy.Stagger > 0)
        {
            _enemy.Stagger--;
            Log($"[color=#888]李无双调息，破绽 -1（→{_enemy.Stagger}）[/color]");
        }
        // stagger cap trigger
        if (_player.Stagger >= _player.MaxStagger)
        {
            _player.MustSkipNextRound = true;
            _player.Stagger = 0;
            Log($"[color=#ff7878]主角破绽 cap！下回合跳过。[/color]");
        }
        if (_enemy.Stagger >= _enemy.MaxStagger)
        {
            _enemy.MustSkipNextRound = true;
            _enemy.Stagger = 0;
            Log($"[color=#ff7878]李无双破绽 cap！下回合跳过。[/color]");
        }

        UpdateUI();
        await Sleep(400);
        CheckGameOver();
    }

    private void CheckGameOver()
    {
        if (!_player.IsAlive)
        {
            _gameOver = true;
            Log("\n[color=#ff5555][b]✘ 主角倒下。[/b][/color]");
            _restartButton.Visible = true;
            ClearMoveButtons();
        }
        else if (!_enemy.IsAlive)
        {
            _gameOver = true;
            Log("\n[color=#ffd040][b]✓ 李无双倒下。[/b][/color]");
            _restartButton.Visible = true;
            ClearMoveButtons();
        }
    }

    // ===================== RESOLUTION =====================
    private async Task Resolve(Move pMove, Move eMove, bool decisive)
    {
        Log($"主角 → [color={SysHex(pMove.System)}]{pMove.SystemName}系 · {pMove.Name}{(decisive ? " (一击决胜)" : "")}[/color]");
        if (decisive)
        {
            await ShowDecisiveBurst();
        }

        // pay costs
        if (decisive)
            _player.NeiXi = Math.Max(0, _player.NeiXi - 3);
        else
            _player.NeiXi = Math.Max(0, _player.NeiXi - pMove.Cost);
        _enemy.NeiXi = Math.Max(0, _enemy.NeiXi - eMove.Cost);

        if (decisive) _player.DecisiveUsed = true;

        // Determine clash outcome (simultaneous)
        // case A: both attack/counter
        // case B: one attacks one defends
        // case C: both defend or info — no damage

        bool pIsAttack = pMove.Category == Cat.Attack || pMove.Category == Cat.Counter;
        bool eIsAttack = eMove.Category == Cat.Attack || eMove.Category == Cat.Counter;
        bool pIsDefense = pMove.Category == Cat.Defense;
        bool eIsDefense = eMove.Category == Cat.Defense;

        if (pIsAttack && eIsAttack)
        {
            // both attack — RPS clash
            var pVsE = ComputeClash(pMove.System, eMove.System); // player's perspective
            var eVsP = ComputeClash(eMove.System, pMove.System); // enemy's perspective

            // Counter override: if player counter targets enemy's system, treat as克制 favor for player
            if (pMove.Category == Cat.Counter && pMove.CountersSystem == eMove.System)
            {
                pVsE = ClashOutcome.Wins;
                eVsP = ClashOutcome.Loses;
                _reactionSucceededThisRound = true;
                Log("[color=#ffd040]反制成功！[/color]");
            }

            // player damages enemy
            int pDmg = ComputeDamage(pMove.Damage, pVsE, decisive);
            ApplyDamage(_enemy, pDmg, pMove, pVsE, isPlayerAttack: true, decisive: decisive);

            // enemy damages player
            int eDmg = ComputeDamage(eMove.Damage, eVsP, decisive: false);
            ApplyDamage(_player, eDmg, eMove, eVsP, isPlayerAttack: false, decisive: false);

            await PopDamageNumber(_enemyCard, pDmg, pVsE == ClashOutcome.Wins, decisive);
            await PopDamageNumber(_playerCard, eDmg, eVsP == ClashOutcome.Wins, false);
        }
        else if (pIsAttack && eIsDefense)
        {
            int dmg = ResolveAttackVsDefense(pMove, eMove, _enemy, decisive);
            await PopDamageNumber(_enemyCard, dmg, false, decisive);
        }
        else if (eIsAttack && pIsDefense)
        {
            int dmg = ResolveAttackVsDefense(eMove, pMove, _player, decisive: false);
            await PopDamageNumber(_playerCard, dmg, false, false);
        }
        else
        {
            Log("[color=#888]双方未交锋。[/color]");
        }

        UpdateUI();
        await Sleep(700);
    }

    private enum ClashOutcome { Wins, Loses, Tie }

    private ClashOutcome ComputeClash(Sys attacker, Sys defender)
    {
        // 刚→巧→柔→刚 (attacker wins if defender is the one it counters)
        if (attacker == defender) return ClashOutcome.Tie;
        bool attackerWins = (attacker == Sys.Gang && defender == Sys.Qiao)
                          || (attacker == Sys.Qiao && defender == Sys.Rou)
                          || (attacker == Sys.Rou && defender == Sys.Gang);
        return attackerWins ? ClashOutcome.Wins : ClashOutcome.Loses;
    }

    private int ComputeDamage(int baseDamage, ClashOutcome outcome, bool decisive)
    {
        double multiplier = outcome switch
        {
            ClashOutcome.Wins => 1.5,
            ClashOutcome.Tie => 1.0,
            ClashOutcome.Loses => 0.5,
            _ => 1.0
        };
        if (decisive) multiplier = 2.5;
        return (int)Math.Floor(baseDamage * multiplier);
    }

    private void ApplyDamage(Combatant target, int dmg, Move attackingMove, ClashOutcome outcome, bool isPlayerAttack, bool decisive)
    {
        if (dmg <= 0) return;
        target.HP = Math.Max(0, target.HP - dmg);
        target.TookDamageThisRound = true;
        if (attackingMove.StaggersOnHit || outcome == ClashOutcome.Loses)
        {
            // attacking move that staggers, OR target was on losing side of clash → stagger
            if (outcome == ClashOutcome.Loses || attackingMove.StaggersOnHit)
            {
                target.Stagger = Math.Min(target.MaxStagger, target.Stagger + 1);
                target.StaggeredThisRound = true;
            }
        }
        if (decisive)
        {
            Log($"  [color=#ffd040]→ {target.Name} 受 {dmg} 伤害 (无视防御)[/color]");
        }
        else
        {
            string clashTag = outcome switch
            {
                ClashOutcome.Wins => " [克制 1.5×]",
                ClashOutcome.Tie => " [硬拼 1×]",
                ClashOutcome.Loses => " [逆向 0.5×]",
                _ => ""
            };
            Log($"  → {target.Name} 受 {dmg} 伤害{clashTag}");
        }
    }

    private int ResolveAttackVsDefense(Move attack, Move defense, Combatant defender, bool decisive)
    {
        // 巧 vs 柔 → 穿透 1×；刚 vs 柔 → 卸力 0× (defender +1 内息)；柔 vs 柔 → 0
        if (decisive)
        {
            int dmg = (int)Math.Floor(attack.Damage * 2.5);
            defender.HP = Math.Max(0, defender.HP - dmg);
            defender.TookDamageThisRound = true;
            Log($"  [color=#ffd040]→ {defender.Name} 受 {dmg} 伤害 (一击决胜 无视防御)[/color]");
            return dmg;
        }
        if (attack.System == Sys.Qiao && defense.System == Sys.Rou)
        {
            int dmg = attack.Damage;
            defender.HP = Math.Max(0, defender.HP - dmg);
            defender.TookDamageThisRound = true;
            Log($"  → 巧穿柔！{defender.Name} 受 {dmg} 伤害");
            return dmg;
        }
        if (attack.System == Sys.Gang && defense.System == Sys.Rou)
        {
            // 卸力 + defender 内息 +1
            defender.NeiXi = Math.Min(defender.MaxNeiXi, defender.NeiXi + 1);
            Log($"  → 柔卸刚！{defender.Name} 卸力 +1 内息");
            return 0;
        }
        if (attack.System == Sys.Rou && defense.System == Sys.Rou)
        {
            Log($"  → 双方僵持。");
            return 0;
        }
        // fallback: defense value blocks all
        Log($"  → {defender.Name} 防御 {defense.DefenseValue} 点");
        return 0;
    }

    private async Task ApplyEnemySoloAttack()
    {
        // when player is staggered out and skips
        var em = _enemyIntent!;
        int dmg = (int)Math.Floor(em.Damage * 1.5); // +50% from stagger trigger
        _player.HP = Math.Max(0, _player.HP - dmg);
        Log($"李无双 {em.Name} → 主角 受 {dmg} 伤害 (破绽 +50%)");
        await PopDamageNumber(_playerCard, dmg, false, false);
        UpdateUI();
    }

    // ===================== PLAYER INPUT =====================
    private Task<(Move move, bool decisive)> WaitForPlayerChoice()
    {
        _playerChoice = new TaskCompletionSource<(Move, bool)>();
        BuildMoveButtons();
        return _playerChoice.Task;
    }

    private void BuildMoveButtons()
    {
        ClearMoveButtons();
        bool decisiveWindowOpen = IsDecisiveWindowOpen();

        if (decisiveWindowOpen && !_player.DecisiveUsed && _player.NeiXi >= 3)
        {
            _decisiveBanner.Text = "★ 一击决胜窗口开启 ★ (任选攻击招式 → 决胜版按钮)";
            _decisiveBanner.AddThemeColorOverride("font_color", new Color("ffd040"));
        }
        else if (decisiveWindowOpen && (_player.DecisiveUsed || _player.NeiXi < 3))
        {
            string reason = _player.DecisiveUsed ? "已用过" : "内息不足 3";
            _decisiveBanner.Text = $"一击决胜窗口开启（不可用：{reason}）";
            _decisiveBanner.AddThemeColorOverride("font_color", new Color("888888"));
        }
        else
        {
            _decisiveBanner.Text = "";
        }

        var row = new HBoxContainer();
        _moveButtonsBox.AddChild(row);

        foreach (var move in _player.Moves)
        {
            bool canAfford = _player.NeiXi >= move.Cost;
            var btn = new Button
            {
                Text = $"{move.SystemName}·{move.Name}\n({move.Cost} 内息)",
                CustomMinimumSize = new Vector2(180, 60),
                Disabled = !canAfford,
            };
            btn.AddThemeColorOverride("font_color", move.SystemColor);
            btn.Pressed += () => _playerChoice?.TrySetResult((move, false));
            row.AddChild(btn);

            // decisive variant if applicable
            if (decisiveWindowOpen && !_player.DecisiveUsed
                && _player.NeiXi >= 3
                && (move.Category == Cat.Attack || move.Category == Cat.Counter))
            {
                var decisiveBtn = new Button
                {
                    Text = $"★ {move.Name}\n(决胜·3 内息)",
                    CustomMinimumSize = new Vector2(140, 60),
                };
                decisiveBtn.AddThemeColorOverride("font_color", new Color("ffd040"));
                decisiveBtn.Pressed += () => _playerChoice?.TrySetResult((move, true));
                row.AddChild(decisiveBtn);
            }
        }
    }

    private void ClearMoveButtons()
    {
        foreach (var c in _moveButtonsBox.GetChildren()) c.QueueFree();
        _decisiveBanner.Text = "";
    }

    private bool IsDecisiveWindowOpen()
    {
        if (_enemy.Stagger >= _enemy.MaxStagger) return true;
        if (_enemy.HP > 0 && _enemy.HP < _enemy.MaxHP * 0.3) return true;
        if (_player.FullHealth && _reactionSucceededLastRound) return true;
        return false;
    }

    // ===================== VISUAL FX =====================
    private async Task FlashIntentReveal()
    {
        var move = _enemyIntent!;
        _enemyIntentLabel.Text = $"{move.SystemName}系 → 主角";
        _enemyIntentLabel.AddThemeColorOverride("font_color", move.SystemColor);

        // flash background
        var origColor = (_enemyIntentLabel.GetParent() as PanelContainer)?.SelfModulate ?? Colors.White;
        var tween = CreateTween();
        tween.TweenProperty(_enemyIntentLabel, "scale", new Vector2(1.3f, 1.3f), 0.12);
        tween.TweenProperty(_enemyIntentLabel, "scale", new Vector2(1.0f, 1.0f), 0.18);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async Task ShowDecisiveBurst()
    {
        _decisiveOverlay.Modulate = new Color(1, 1, 1, 0);
        _decisiveOverlay.Scale = new Vector2(0.5f, 0.5f);
        _decisiveOverlay.PivotOffset = _decisiveOverlay.Size / 2;
        var t = CreateTween().SetParallel(true);
        t.TweenProperty(_decisiveOverlay, "modulate:a", 1.0, 0.18);
        t.TweenProperty(_decisiveOverlay, "scale", new Vector2(1.4f, 1.4f), 0.25);

        // bg flash
        var bgFlash = new ColorRect { Color = new Color(1, 1, 1, 0.0f) };
        bgFlash.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        bgFlash.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(bgFlash);
        MoveChild(bgFlash, _decisiveOverlay.GetIndex());
        var bgT = CreateTween();
        bgT.TweenProperty(bgFlash, "color:a", 0.7, 0.08);
        bgT.TweenProperty(bgFlash, "color:a", 0.0, 0.25);

        await ToSignal(GetTree().CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);

        var fade = CreateTween();
        fade.TweenProperty(_decisiveOverlay, "modulate:a", 0.0, 0.35);
        await ToSignal(fade, Tween.SignalName.Finished);
        bgFlash.QueueFree();
    }

    private async Task PopDamageNumber(Control targetCard, int dmg, bool isCounter, bool isDecisive)
    {
        if (dmg <= 0) return;
        var lbl = new Label
        {
            Text = $"-{dmg}",
            Position = targetCard.GlobalPosition + new Vector2(targetCard.Size.X / 2 - 20, 0),
        };
        int fontSize = isDecisive ? 56 : (isCounter ? 38 : 28);
        lbl.AddThemeFontSizeOverride("font_size", fontSize);
        var color = isDecisive ? new Color("ffd040") : (isCounter ? new Color("ff8a3a") : new Color("ffffff"));
        lbl.AddThemeColorOverride("font_color", color);
        _damageLayer.AddChild(lbl);

        var tween = CreateTween().SetParallel(true);
        tween.TweenProperty(lbl, "position:y", lbl.Position.Y - 80, 0.7);
        tween.TweenProperty(lbl, "modulate:a", 0.0, 0.7).SetDelay(0.2);

        // small shake of the target card
        var origPos = targetCard.Position;
        var shakeT = CreateTween();
        shakeT.TweenProperty(targetCard, "position", origPos + new Vector2(6, 0), 0.04);
        shakeT.TweenProperty(targetCard, "position", origPos + new Vector2(-6, 0), 0.04);
        shakeT.TweenProperty(targetCard, "position", origPos, 0.04);

        await ToSignal(tween, Tween.SignalName.Finished);
        lbl.QueueFree();
    }

    private async Task Sleep(int ms)
    {
        await ToSignal(GetTree().CreateTimer(ms / 1000.0), SceneTreeTimer.SignalName.Timeout);
    }

    // ===================== UI UPDATE =====================
    private void UpdateUI()
    {
        _playerStats.Text = $"HP: {_player.HP}/{_player.MaxHP}\n内息: {_player.NeiXi}/{_player.MaxNeiXi}\n破绽: {_player.Stagger}/{_player.MaxStagger}";
        _enemyStats.Text = $"HP: {_enemy.HP}/{_enemy.MaxHP}\n内息: {_enemy.NeiXi}/{_enemy.MaxNeiXi}\n破绽: {_enemy.Stagger}/{_enemy.MaxStagger}";
        _roundLabel.Text = $"回合 {_round}";
    }

    private void Log(string msg)
    {
        _log.AppendText(msg + "\n");
    }

    private string SysHex(Sys s) => s switch
    {
        Sys.Gang => "e85a4d",
        Sys.Rou => "5b9fdb",
        Sys.Qiao => "7ac765",
        _ => "cccccc"
    };
    private string EnemyIntentColorHex() => SysHex(_enemyIntent?.System ?? Sys.Gang);

    // ===================== ACTORS =====================
    private Combatant MakePlayer()
    {
        var c = new Combatant
        {
            Name = "主角",
            MaxHP = 20, HP = 20,
            NeiXi = 3, MaxNeiXi = 5,
        };
        c.Moves.Add(new Move { Name = "白虹贯日", System = Sys.Gang, Category = Cat.Attack, Cost = 1, Damage = 4 });
        c.Moves.Add(new Move { Name = "守拙式", System = Sys.Rou, Category = Cat.Defense, Cost = 0, DefenseValue = 4 });
        c.Moves.Add(new Move { Name = "回风落雁", System = Sys.Qiao, Category = Cat.Counter, Cost = 3, Damage = 6, StaggersOnHit = true, CountersSystem = Sys.Gang });
        return c;
    }

    private Combatant MakeEnemy()
    {
        var c = new Combatant
        {
            Name = "李无双",
            MaxHP = 18, HP = 18,
            NeiXi = 3, MaxNeiXi = 5,
        };
        c.Moves.Add(new Move { Name = "追亡剑", System = Sys.Gang, Category = Cat.Attack, Cost = 1, Damage = 3 });
        c.Moves.Add(new Move { Name = "千里追风", System = Sys.Gang, Category = Cat.Attack, Cost = 2, Damage = 5, StaggersOnHit = true });
        c.Moves.Add(new Move { Name = "避影身", System = Sys.Rou, Category = Cat.Defense, Cost = 0, DefenseValue = 4 });
        return c;
    }

    private Move PickEnemyMove()
    {
        // simple AI: pick by available NeiXi, slight randomness
        var rng = new Random();
        var available = _enemy.Moves.Where(m => _enemy.NeiXi >= m.Cost).ToList();
        if (available.Count == 0) return _enemy.Moves.First(m => m.Cost == 0);

        // if low NeiXi, defend
        if (_enemy.NeiXi < 2 && available.Any(m => m.Category == Cat.Defense))
        {
            return available.First(m => m.Category == Cat.Defense);
        }
        // if player has high stagger, go big
        if (_player.Stagger >= 2 && available.Any(m => m.Name == "千里追风"))
            return available.First(m => m.Name == "千里追风");

        // weighted random among attacks
        var attacks = available.Where(m => m.Category == Cat.Attack).ToList();
        if (attacks.Count == 0) return available[0];
        return attacks[rng.Next(attacks.Count)];
    }
}
