using System.Collections.Generic;
using System.Linq;
using FengZhi.Foundation.Exploration.Interaction;
using Godot;

namespace FengZhi.Scripts.Exploration;

/// <summary>
/// 通用交互控制器节点。
///
/// 设计意图：
///   - 挂在任意场景作子节点，通过 NodePath 导出获取依赖（Player / UI / sprite root）
///   - 不绑定具体场景基类，纯组合复用
///   - 场景脚本通过 RegisterZone / RegisterOneShotZone 注册交互点
///   - 注册完后 controller 自动延迟（CallDeferred）执行 outline 匹配 + 已领取 zone 的视觉应用
///
/// 适用范围：任何包含 Player + Area2D + 可选 sprite-based 视觉层的场景，无论是否使用 TileMap。
/// </summary>
public partial class InteractionController : Node
{
	[Export] public NodePath PlayerPath = "";
	[Export] public NodePath PromptLabelPath = "";
	[Export] public NodePath MessagePanelPath = "";
	[Export] public NodePath MessageLabelPath = "";

	/// <summary>用于 outline 匹配的 sprite 容器（如 RoomVisual 节点）；为空则跳过 outline 功能。</summary>
	[Export] public NodePath SpriteRootPath = "";

	[Export] public string OutlineShaderPath = "res://assets/shaders/interactable_outline.gdshader";

	/// <summary>逗号分隔的 tileset_id 片段，匹配到的 sprite 视为非交互背景（地板/墙等）。</summary>
	[Export] public string ExcludedTilesetMarkers = "62fcaf44684e4323bb29b91a"; // iso 房间默认地板 tileset hash 片段

	[Export] public float OutlineAnchorMaxDistance = 200f;
	[Export] public float SpawnCooldownSeconds = 1.0f;
	[Export] public string InteractActionName = "interact";

	private Node2D _player = null!;
	private Label _promptLabel = null!;
	private Panel _messagePanel = null!;
	private Label _messageLabel = null!;
	private Node? _spriteRoot;
	private Shader? _outlineShader;

	private readonly List<InteractZone> _zones = new();
	private InteractZone? _focusedZone;
	private float _spawnCooldown;
	private string[] _excludedMarkers = System.Array.Empty<string>();
	private bool _finalizationQueued;

	/// <summary>玩家是否当前被一次性弹窗冻结。供场景脚本查询。</summary>
	public bool PlayerFrozen => _messagePanel.Visible;

	public override void _Ready()
	{
		_player = GetNode<Node2D>(PlayerPath);
		_promptLabel = GetNode<Label>(PromptLabelPath);
		_messagePanel = GetNode<Panel>(MessagePanelPath);
		_messageLabel = GetNode<Label>(MessageLabelPath);
		_spriteRoot = string.IsNullOrEmpty(SpriteRootPath) ? null : GetNodeOrNull(SpriteRootPath);
		_outlineShader = ResourceLoader.Exists(OutlineShaderPath)
			? GD.Load<Shader>(OutlineShaderPath)
			: null;
		_excludedMarkers = ExcludedTilesetMarkers
			.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
			.ToArray();
		_spawnCooldown = SpawnCooldownSeconds;
	}

	// ============================================================
	// 注册 API（供场景脚本调用）
	// ============================================================

	/// <summary>注册一个普通交互点（可重复触发）。</summary>
	public void RegisterZone(
		string areaNodeName,
		string prompt,
		System.Action onInteract,
		bool supportsOutline = true)
	{
		var area = GetParentArea(areaNodeName);
		if (area == null) return;

		var zone = new InteractZone
		{
			Area = area,
			PromptProvider = () => prompt,
			OnInteract = onInteract,
			SupportsOutline = supportsOutline,
		};
		_zones.Add(zone);
		area.BodyEntered += body => OnZoneEntered(zone, body);
		area.BodyExited += body => OnZoneExited(zone, body);
		QueueFinalization();
	}

	/// <summary>
	/// 注册一个"一次性领取"交互点。
	/// 已领取状态 → 注册为 stub（area 禁用），但 outline targets 仍会被匹配以应用 claimedEffect。
	/// </summary>
	public void RegisterOneShotZone(
		string id,
		string areaNodeName,
		string prompt,
		string claimMessage,
		ClaimedEffect claimedEffect = ClaimedEffect.None,
		Texture2D? frameSwapTexture = null)
	{
		var area = GetParentArea(areaNodeName);
		if (area == null) return;

		if (claimedEffect == ClaimedEffect.FrameSwap && frameSwapTexture == null)
		{
			GD.PushWarning($"[Interaction] '{id}' 配置 FrameSwap 但未提供 frameSwapTexture，将退化为 FadeOut。");
			claimedEffect = ClaimedEffect.FadeOut;
		}

		bool alreadyClaimed = OneShotClaimRegistry.IsClaimed(id);

		// 先用 placeholder OnInteract 创建 zone，再 reassign 让 lambda 能引用 zone。
		var zone = new InteractZone
		{
			Area = area,
			PromptProvider = () => prompt,
			OnInteract = () => { },
			ClaimedEffect = claimedEffect,
			FrameSwapTexture = frameSwapTexture,
			IsClaimedStub = alreadyClaimed,
			SupportsOutline = !alreadyClaimed,
		};
		zone.OnInteract = () =>
		{
			OneShotClaimRegistry.MarkClaimed(id);
			GD.Print($"[Interaction] One-shot claimed: '{id}' (effect={claimedEffect}).");
			ShowMessage(claimMessage);
			if (_focusedZone?.Area == area)
			{
				SetOutlineEnabled(_focusedZone, false);
				_focusedZone = null;
			}
			DisableArea(area);
			_promptLabel.Visible = false;
			ClaimedEffectApplier.Apply(zone);
		};

		_zones.Add(zone);

		if (alreadyClaimed)
		{
			DisableArea(area);
			GD.Print($"[Interaction] One-shot already claimed at load: '{id}' (effect={claimedEffect}).");
		}
		else
		{
			area.BodyEntered += body => OnZoneEntered(zone, body);
			area.BodyExited += body => OnZoneExited(zone, body);
		}
		QueueFinalization();
	}

	private Area2D? GetParentArea(string areaNodeName)
	{
		var parent = GetParent();
		var area = parent.GetNodeOrNull<Area2D>(areaNodeName);
		if (area == null)
			GD.PushWarning($"[Interaction] 未找到 Area2D 节点 '{areaNodeName}' (在 {parent.Name} 下)。");
		return area;
	}

	// ============================================================
	// Outline 自动装配（首个 Register 后延迟到本帧末尾执行）
	// ============================================================

	private void QueueFinalization()
	{
		if (_finalizationQueued) return;
		_finalizationQueued = true;
		CallDeferred(nameof(FinalizeOutlines));
	}

	private void FinalizeOutlines()
	{
		if (_spriteRoot == null)
		{
			GD.Print($"[Interaction] {GetParent().Name}: SpriteRootPath 未设置，跳过 outline 匹配。");
			return;
		}

		var allSprites = new List<Sprite2D>();
		OutlineMatcher.CollectSprites(_spriteRoot, allSprites);
		var anchorCandidates = allSprites
			.Where(s => !OutlineMatcher.IsExcludedByMarkers(s, _excludedMarkers))
			.ToList();

		GD.Print($"[Interaction] {GetParent().Name}: total sprites={allSprites.Count}, " +
			$"anchor candidates (non-excluded)={anchorCandidates.Count}, zones={_zones.Count}.");

		foreach (var zone in _zones)
		{
			if (!zone.SupportsOutline && !zone.IsClaimedStub) continue;

			var result = OutlineMatcher.Match(zone, allSprites, anchorCandidates, OutlineAnchorMaxDistance);
			LogMatch(zone, result);
		}

		// Outline shader 只挂到"未领取且支持高亮"的 zone 的 sprite。
		if (_outlineShader != null)
		{
			var activeTargets = _zones
				.Where(z => z.SupportsOutline && !z.IsClaimedStub)
				.SelectMany(z => z.OutlineTargets)
				.Distinct()
				.ToList();
			foreach (var sp in activeTargets)
			{
				var mat = new ShaderMaterial { Shader = _outlineShader };
				mat.SetShaderParameter("enabled", false);
				sp.Material = mat;
			}
			GD.Print($"[Interaction] {GetParent().Name}: active outline material sprites={activeTargets.Count}.");
		}
		else
		{
			GD.PushWarning($"[Interaction] outline shader 未加载（{OutlineShaderPath}）；将跳过描边效果。");
		}

		// 已领取 zone 立即应用持久视觉状态。
		foreach (var zone in _zones.Where(z => z.IsClaimedStub))
		{
			ClaimedEffectApplier.Apply(zone);
		}
	}

	private static void LogMatch(InteractZone zone, OutlineMatcher.MatchResult r)
	{
		var areaName = zone.Area.Name;
		switch (r.Mode)
		{
			case OutlineMatcher.MatchMode.Manual:
				GD.Print($"[Interaction]   zone '{areaName}' MANUAL outline: {r.Count} sprites.");
				break;
			case OutlineMatcher.MatchMode.Skipped:
				GD.Print($"[Interaction]   zone '{areaName}' SKIPPED outline (anchor dist={r.AnchorDistance:F0})。");
				break;
			case OutlineMatcher.MatchMode.AnchorOnly:
				GD.Print($"[Interaction]   zone '{areaName}' ANCHOR-ONLY outline (anchor dist={r.AnchorDistance:F0})。");
				break;
			case OutlineMatcher.MatchMode.AutoByTilesetHash:
				var shortHash = r.BaseTilesetHash != null && r.BaseTilesetHash.Length > 12
					? r.BaseTilesetHash[^12..]
					: r.BaseTilesetHash ?? "?";
				GD.Print($"[Interaction]   zone '{areaName}' AUTO outline by tsid '...{shortHash}': {r.Count} sprites " +
					$"(anchor dist={r.AnchorDistance:F0}).");
				break;
		}
	}

	// ============================================================
	// 进入 / 退出 / 输入
	// ============================================================

	private void OnZoneEntered(InteractZone zone, Node2D body)
	{
		if (body != _player) return;
		_focusedZone = zone;
		if (_spawnCooldown <= 0f && !_messagePanel.Visible)
		{
			_promptLabel.Text = zone.PromptProvider();
			_promptLabel.Visible = true;
			SetOutlineEnabled(zone, true);
		}
	}

	private void OnZoneExited(InteractZone zone, Node2D body)
	{
		if (body != _player || _focusedZone != zone) return;
		_focusedZone = null;
		_promptLabel.Visible = false;
		SetOutlineEnabled(zone, false);
	}

	public override void _Process(double delta)
	{
		if (_spawnCooldown > 0f)
		{
			_spawnCooldown -= (float)delta;
			if (_spawnCooldown <= 0f && _focusedZone != null && !_messagePanel.Visible)
			{
				_promptLabel.Text = _focusedZone.PromptProvider();
				_promptLabel.Visible = true;
				SetOutlineEnabled(_focusedZone, true);
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed(InteractActionName) && !@event.IsActionPressed("ui_accept"))
			return;

		if (_messagePanel.Visible)
		{
			DismissMessage();
			return;
		}

		if (_focusedZone != null && _spawnCooldown <= 0f)
			_focusedZone.OnInteract();
	}

	// ============================================================
	// 消息面板（供场景脚本调用，e.g. OpenAlchemyInterface 弹 stub 文案）
	// ============================================================

	public void ShowMessage(string text)
	{
		_messageLabel.Text = text;
		_messagePanel.Visible = true;
		_promptLabel.Visible = false;
		FreezePlayer(true);
		if (_focusedZone != null)
			SetOutlineEnabled(_focusedZone, false);
	}

	private void DismissMessage()
	{
		_messagePanel.Visible = false;
		FreezePlayer(false);
		if (_focusedZone != null)
		{
			_promptLabel.Text = _focusedZone.PromptProvider();
			_promptLabel.Visible = true;
			SetOutlineEnabled(_focusedZone, true);
		}
	}

	/// <summary>
	/// 冻结/解冻玩家移动。优先调用 CavePlayer.MovementFrozen，
	/// 找不到则尝试用反射设置同名 property（保持解耦）。
	/// </summary>
	private void FreezePlayer(bool frozen)
	{
		var prop = _player.GetType().GetProperty("MovementFrozen");
		if (prop != null && prop.CanWrite)
		{
			prop.SetValue(_player, frozen);
		}
	}

	// ============================================================
	// Outline shader 开关 + Area 禁用
	// ============================================================

	private static void SetOutlineEnabled(InteractZone zone, bool enabled)
	{
		foreach (var sp in zone.OutlineTargets)
		{
			if (sp.Material is ShaderMaterial mat)
				mat.SetShaderParameter("enabled", enabled);
		}
	}

	private static void DisableArea(Area2D area)
	{
		area.Monitoring = false;
		area.Monitorable = false;
		area.Visible = false;
	}
}
