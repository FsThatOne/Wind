using System.Collections.Generic;
using FengZhi.Foundation.Dialogue;
using FengZhi.Foundation.Mindset;

namespace FengZhi.Dialogue;

public sealed class CaveConditionValueProvider : IDialogueConditionValueProvider
{
	private readonly MindsetService _mindsetService;
	private readonly Dictionary<string, string> _flags = new(System.StringComparer.Ordinal);

	public CaveConditionValueProvider(MindsetService mindsetService)
	{
		_mindsetService = mindsetService;
	}

	public void SetFlag(string key, string value) => _flags[key] = value;

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
			default:
				error = $"Unknown condition source: '{condition.Source}'";
				return false;
		}
	}
}
