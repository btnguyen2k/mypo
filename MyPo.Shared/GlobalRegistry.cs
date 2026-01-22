using System.Text.Json;

namespace MyPo.Shared.Global;

/// <summary>
/// Global registry to hold global/shared objects.
/// </summary>
public sealed partial class GlobalRegistry
{
	public static readonly Dictionary<string, object> TOGGLE_FLAGS = [];

	public static bool EnableToggleFlag(string flag)
	{
		var result = TOGGLE_FLAGS.TryAdd(flag, true);
		Console.WriteLine($"[==========] Enabled toggle flag: {flag}, result: {JsonSerializer.Serialize(TOGGLE_FLAGS)}");
		return result;
	}

	public static bool DisableToggleFlag(string flag)
	{
		var result = TOGGLE_FLAGS.Remove(flag);
		Console.WriteLine($"[==========] Disabled toggle flag: {flag}, result: {JsonSerializer.Serialize(TOGGLE_FLAGS)}");
		return result;
	}

	public static bool IsToggleFlagEnabled(string flag)
	{
		return TOGGLE_FLAGS.ContainsKey(flag);
	}
}
