using System.Collections.Generic;

namespace FengZhi.Foundation.Exploration.Interaction;

/// <summary>
/// 一次性领取点的全局已领取记录。
///
/// 设计意图：
///   - v0：进程内 in-memory HashSet（同一次游戏运行有效，退出后重置）
///   - 接 SaveSystem 时只动此文件：
///       · 存档：SaveManager 调 Snapshot() 取全集，写入存档 payload
///       · 读档：SaveManager 调 Restore(savedIds) 写回
///       · 实时持久（可选）：在 MarkClaimed 内触发增量存档
///   - 新游戏开局：调 Clear()
///
/// id 命名规范：建议 "&lt;scene&gt;.&lt;object&gt;"，如 "alchemy_room.treasure_chest"。
/// 全局唯一即可，没有强制层级约束。
/// </summary>
public static class OneShotClaimRegistry
{
	private static readonly HashSet<string> _claimed = new();

	public static bool IsClaimed(string id) => _claimed.Contains(id);

	public static void MarkClaimed(string id)
	{
		_claimed.Add(id);
		// TODO: 接入 SaveManager 后，在此触发增量持久化（或交由调用方批量保存）。
	}

	/// <summary>取全部已领取 id 的快照（用于存档序列化）。</summary>
	public static IReadOnlyCollection<string> Snapshot() => _claimed;

	/// <summary>从存档恢复已领取 id（先清空再写入）。</summary>
	public static void Restore(IEnumerable<string> claimedIds)
	{
		_claimed.Clear();
		foreach (var id in claimedIds) _claimed.Add(id);
	}

	/// <summary>新游戏 / 测试用。生产场景应通过 Restore(空集合) 实现。</summary>
	public static void Clear() => _claimed.Clear();
}
