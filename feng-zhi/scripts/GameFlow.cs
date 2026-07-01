using System;
using System.Collections.Generic;
using System.Linq;
using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat;
using FengZhi.Foundation.Data;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.Mindset;
using Godot;

namespace FengZhi;

/// <summary>
/// 游戏流程控制器（Autoload 单例）。
///
/// 持有跨场景共享的核心服务：EventBus、MindsetService、DataRegistry、
/// CharacterRegistry、PlayerInstance。
/// 场景切换的转场逻辑由各场景自行管理（参见 SceneTransitionManager），
/// 本控制器只负责全局状态的持有和心境位移的计算。
///
/// 装载方式：作为 Autoload 注册（Project Settings → Autoload，路径 /root/GameFlow）。
/// </summary>
public partial class GameFlow : Node
{
	public enum TrackedQuestKind
	{
		Mainline,
		Side,
		Tutorial,
	}

	public sealed record TrackedQuestObjective(
		string Id,
		string Chapter,
		string Text,
		string Reason,
		TrackedQuestKind Kind,
		long Sequence);

	/// <summary>玩家在战斗结算做出的心境选择。</summary>
	public enum MindsetChoice
	{
		None,
		Spare,       // 放过：心境 → 仁
		Defeat,      // 重伤：心境 → 狠
	}

	public MindsetChoice LastMindsetChoice { get; private set; } = MindsetChoice.None;
	public bool HasCompletedBattleOnce { get; private set; }

	public bool IsInCombat { get; set; }
	public bool IsInCinematicLock { get; set; }
	public int TextCharsPerSecond { get; set; } = 30;

	/// <summary>上一场战斗结果（仅 outcome scene 期间有意义）。</summary>
	public BattleResult LastBattleResult { get; private set; } = BattleResult.InProgress;

	/// <summary>
	/// 全局共享的 EventBus（mindset / battle ui 等跨场景事件总线）。
	///
	/// 由 Autoload _Ready 创建并存活整个进程；scene transition 不会清掉
	/// （Autoload 节点不会随 ChangeSceneToFile 重建）。
	/// </summary>
	public IEventBus EventBus { get; private set; } = null!;

	/// <summary>
	/// 全局玩家心境状态服务。
	///
	/// 单例：所有 outcome 选择都通过这一份 MindsetService 累积位移，
	/// scene transition 不丢状态。整局会话内存 only。
	/// </summary>
	public MindsetService MindsetService { get; private set; } = null!;

	/// <summary>数据注册中心（角色模板等）</summary>
	public DataRegistry DataRegistry { get; private set; } = null!;

	/// <summary>角色注册表</summary>
	public ICharacterRegistry CharacterRegistry { get; private set; } = null!;

	/// <summary>玩家角色运行时实例</summary>
	public CharacterInstance PlayerInstance { get; private set; } = null!;

	private readonly Dictionary<string, string> _questFlags = new(StringComparer.Ordinal);
	private readonly List<TrackedQuestObjective> _trackedObjectives = new();
	private long _trackedObjectiveSequence;

	/// <summary>
	/// 当前运行会话内的剧情 flag。用于 Godot 场景之间传递轻量主线进度；
	/// 正式存档落地后应接入 SaveSystem 的 narrative payload。
	/// </summary>
	public IReadOnlyDictionary<string, string> QuestFlags => _questFlags;

	public IReadOnlyList<TrackedQuestObjective> GetTrackedObjectivesForHud()
	{
		var result = new List<TrackedQuestObjective>(capacity: 3);
		var latestMainline = _trackedObjectives
			.Where(objective => objective.Kind == TrackedQuestKind.Mainline)
			.OrderByDescending(objective => objective.Sequence)
			.FirstOrDefault();
		if (latestMainline != null)
			result.Add(latestMainline);

		foreach (var objective in _trackedObjectives
			.Where(objective => objective.Kind != TrackedQuestKind.Mainline)
			.OrderByDescending(objective => objective.Sequence))
		{
			if (result.Count >= 3)
				break;

			result.Add(objective);
		}

		return result;
	}

	/// <summary>
	/// 上一次 outcome 选择产生的心境位移快照（含 oldState / newState / 应用的 deltas）。
	/// OutcomeGame UI 读取此快照渲染朦胧化前后对比文本。
	/// 在 ApplyOutcomeChoice 时刷新。
	/// </summary>
	public MindsetShiftSnapshot? LastMindsetShiftSnapshot { get; private set; }

	public override void _Ready()
	{
		EventBus = new EventBus();
		MindsetService = new MindsetService(eventBus: EventBus);

		DataRegistry = new DataRegistry();
		var loader = new CharacterConfigLoader();
		var yamlSources = LoadCharacterYamlFiles("res://assets/data/characters");
		loader.LoadAllTemplates(yamlSources, DataRegistry);

		var registry = new CharacterRegistry(DataRegistry, EventBus);
		CharacterRegistry = registry;
		PlayerInstance = registry.CreatePlayer();

		GD.Print($"[GameFlow] Boot. EventBus + MindsetService autoload ready. " +
				 $"Initial state R={MindsetService.State.Resolve} W={MindsetService.State.Worldly} M={MindsetService.State.Morality}");
		GD.Print($"[GameFlow] PlayerInstance created: {PlayerInstance.RuntimeId} " +
				 $"HP={PlayerInstance.GetMaxHp()} NeiXi={PlayerInstance.GetMaxNeiXi()} " +
				 $"STR={PlayerInstance.Attributes.Strength} AGI={PlayerInstance.Attributes.Agility} " +
				 $"INP={PlayerInstance.Attributes.InnerPower} INS={PlayerInstance.Attributes.Insight} " +
				 $"CON={PlayerInstance.Attributes.Constitution}");
	}

	private static Dictionary<string, string> LoadCharacterYamlFiles(string dirPath)
	{
		var result = new Dictionary<string, string>();
		var dir = DirAccess.Open(dirPath);
		if (dir == null)
		{
			GD.PrintErr($"[GameFlow] Cannot open directory: {dirPath}");
			return result;
		}

		dir.ListDirBegin();
		var fileName = dir.GetNext();
		while (!string.IsNullOrEmpty(fileName))
		{
			if (!dir.CurrentIsDir() && fileName.EndsWith(".yaml"))
			{
				var fullPath = $"{dirPath}/{fileName}";
				var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Read);
				if (file != null)
				{
					result[fullPath] = file.GetAsText();
					file.Close();
				}
			}
			fileName = dir.GetNext();
		}
		dir.ListDirEnd();
		return result;
	}

	/// <summary>
	/// 记录战斗结果，供 outcome scene 读取。
	/// </summary>
	public void SetLastBattleResult(BattleResult result)
	{
		LastBattleResult = result;
		HasCompletedBattleOnce = true;
		GD.Print($"[GameFlow] LastBattleResult = {result}");
	}

	public void RecordMindsetChoice(MindsetChoice choice)
	{
		LastMindsetChoice = choice;
		GD.Print($"[GameFlow] Mindset choice recorded: {choice}");
	}

	public void RecordQuestFlag(string key, string? value)
	{
		if (string.IsNullOrWhiteSpace(key))
			return;

		var normalized = string.IsNullOrWhiteSpace(value) ? "true" : value;
		_questFlags[key] = normalized;
		GD.Print($"[GameFlow] Quest flag: {key}={normalized}");
	}

	public bool HasQuestFlag(string key, string expectedValue = "true")
		=> _questFlags.TryGetValue(key, out var value) &&
			string.Equals(value, expectedValue, StringComparison.Ordinal);

	public string? GetQuestFlag(string key)
		=> _questFlags.TryGetValue(key, out var value) ? value : null;

	public void TrackObjective(
		string chapter,
		string objective,
		string reason = "",
		TrackedQuestKind kind = TrackedQuestKind.Mainline,
		string? id = null)
	{
		if (string.IsNullOrWhiteSpace(objective))
			return;

		var normalizedId = string.IsNullOrWhiteSpace(id)
			? $"{kind}:{chapter}:{objective}"
			: id;
		_trackedObjectives.RemoveAll(existing => string.Equals(existing.Id, normalizedId, StringComparison.Ordinal));
		_trackedObjectives.Add(new TrackedQuestObjective(
			normalizedId,
			chapter,
			objective,
			reason,
			kind,
			++_trackedObjectiveSequence));

		if (_trackedObjectives.Count > 12)
			_trackedObjectives.RemoveRange(0, _trackedObjectives.Count - 12);
	}

	/// <summary>
	/// 应用 outcome scene 玩家选择产生的心境位移。
	///
	/// 数值来源：dev-story spec §3（owner sign-off）。
	///   - Spare  → Morality +5（关键善行）+ Resolve +2（放下一寸）
	///   - Defeat → Morality -5（关键恶行）+ Resolve -2（执念加深）
	///   - None / 未识别 → 不位移
	///
	/// 位移通过 MindsetService.ApplyShifts 批量应用；EventBus 会广播每个
	/// <see cref="MindsetShiftedEvent"/>（一次调用可能发 2 个），并在区域 / 道义档位变化时
	/// 发 <see cref="MindsetZoneChangedEvent"/> / <see cref="MoralityTierChangedEvent"/>。
	///
	/// 同步刷新 LastMindsetShiftSnapshot，供 UI 渲染前后对比文本。
	/// </summary>
	public void ApplyOutcomeChoice(MindsetChoice choice)
	{
		LastMindsetChoice = choice;

		var shifts = ResolveShifts(choice);
		if (shifts.Count == 0)
		{
			GD.Print($"[GameFlow] Outcome choice {choice}: no shifts to apply.");
			LastMindsetShiftSnapshot = null;
			return;
		}

		var oldState = MindsetService.State;
		var oldZone = MindsetService.CurrentZone;
		var oldTier = MindsetService.CurrentMoralityTier;

		MindsetService.ApplyShifts(shifts);

		var newState = MindsetService.State;
		var newZone = MindsetService.CurrentZone;
		var newTier = MindsetService.CurrentMoralityTier;

		LastMindsetShiftSnapshot = new MindsetShiftSnapshot(
			Choice: choice,
			OldState: oldState,
			NewState: newState,
			OldZone: oldZone,
			NewZone: newZone,
			OldMoralityTier: oldTier,
			NewMoralityTier: newTier,
			AppliedShifts: shifts);

		GD.Print(
			$"[GameFlow] Outcome choice {choice} applied: " +
			$"R {oldState.Resolve}→{newState.Resolve}, " +
			$"W {oldState.Worldly}→{newState.Worldly}, " +
			$"M {oldState.Morality}→{newState.Morality}; " +
			$"zone {oldZone}→{newZone}; tier {oldTier}→{newTier}");
	}

	/// <summary>
	/// dev-story spec §3 规约的 choice → shifts 映射，转发到 Foundation
	/// 单一权威实现 <see cref="MindsetOutcomeShifts.ResolveShifts"/>。
	/// </summary>
	public static IReadOnlyList<MindsetShift> ResolveShifts(MindsetChoice choice)
	{
		return MindsetOutcomeShifts.ResolveShifts(ToOutcomeChoice(choice));
	}

	private static MindsetOutcomeChoice ToOutcomeChoice(MindsetChoice choice)
	{
		return choice switch
		{
			MindsetChoice.Spare => MindsetOutcomeChoice.Spare,
			MindsetChoice.Defeat => MindsetOutcomeChoice.Defeat,
			_ => MindsetOutcomeChoice.None,
		};
	}
}

/// <summary>
/// 战斗结算一次心境位移的完整快照。
///
/// 字段一次性冻结调用 <see cref="GameFlow.ApplyOutcomeChoice"/> 前后的状态，
/// 让 UI 不依赖事件顺序就能渲染朦胧化「前 → 后」对比展示。
/// </summary>
public sealed record MindsetShiftSnapshot(
	GameFlow.MindsetChoice Choice,
	MindsetState OldState,
	MindsetState NewState,
	MindsetZone OldZone,
	MindsetZone NewZone,
	MoralityTier OldMoralityTier,
	MoralityTier NewMoralityTier,
	IReadOnlyList<MindsetShift> AppliedShifts);
