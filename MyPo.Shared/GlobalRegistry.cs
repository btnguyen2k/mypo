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
		return result;
	}

	public static bool DisableToggleFlag(string flag)
	{
		var result = TOGGLE_FLAGS.Remove(flag);
		return result;
	}

	public static bool IsToggleFlagEnabled(string flag)
	{
		return TOGGLE_FLAGS.ContainsKey(flag);
	}

	/*----------------------------------------------------------------------*/

	public static readonly Dictionary<string, ISet<string>> INDEX_CONSTITUENTS = new(StringComparer.OrdinalIgnoreCase);
}
