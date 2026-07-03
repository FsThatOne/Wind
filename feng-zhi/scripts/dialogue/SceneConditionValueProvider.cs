using System.Collections.Generic;
using FengZhi.Foundation.Dialogue;
using FengZhi.Foundation.Mindset;

namespace FengZhi.Dialogue;

public sealed class SceneConditionValueProvider : IDialogueConditionValueProvider
{
	private readonly MindsetService _mindsetService;
	private readonly Dictionary<string, string> _flags = new(System.StringComparer.Ordinal);
	private readonly Dictionary<string, int> _misunderstandingMods = new(System.StringComparer.Ordinal);

	public SceneConditionValueProvider(MindsetService mindsetService)
	{
		_mindsetService = mindsetService;
	}

	public void SetFlag(string key, string value) => _flags[key] = value;

	public void SetMisunderstandingMod(string npcId, int value) => _misunderstandingMods[npcId] = value;

	public bool TryGetValue(DialogueConditionSpec condition, out string? value, out string? error)
	{
		error = null;
		value = null;

		switch (condition.Source)
		{
			case "mindset.resolve":
				value = _mindsetService.State.Resolve.ToString();
				return true;
			case "mindset.worldly":
				value = _mindsetService.State.Worldly.ToString();
				return true;
			case "mindset.morality":
				value = _mindsetService.State.Morality.ToString();
				return true;
			case "flag":
				if (_flags.TryGetValue(condition.Key ?? "", out var flagValue))
				{
					value = flagValue;
				}
				else
				{
					value = "false";
				}
				return true;
			case { } source when source.StartsWith("misunderstanding_mod.", System.StringComparison.Ordinal):
				var npcId = source["misunderstanding_mod.".Length..];
				value = _misunderstandingMods.TryGetValue(npcId, out var mod)
					? mod.ToString(System.Globalization.CultureInfo.InvariantCulture)
					: "0";
				return true;
			default:
				error = $"Unknown condition source: '{condition.Source}'";
				return false;
		}
	}
}
