using Microsoft.AspNetCore.Components;
using MyPo.Blazor.App.Shared;

namespace MyPo.Blazor.App.Layout;

public partial class Sidebar
{
    private CModal ModalDebug { get; set; } = default!;

    /// <summary>
    /// Delegate that performs the actual "Debug" action.
    /// </summary>
    /// <remarks>
    /// This project (MyPo.Blazor.App) is the base UI layer and does not reference the portfolio module.
    /// Downstream modules (e.g. MyPo.Blazor.Portfolio.App) register a handler that calls the server-side
    /// debug API. The handler receives an <see cref="IServiceProvider"/> so it can resolve the services it
    /// needs (API client, local storage, ...) and returns the lines of output to display.
    /// </remarks>
    public static Func<IServiceProvider, Task<string[]>>? DebugHandler { get; set; }

    [Inject]
    protected IServiceProvider ServiceProvider { get; init; } = default!;

    private bool DebugRunning { get; set; } = false;
    private string[]? DebugOutput { get; set; } = null;

    private void OpenDebugDialog()
    {
        DebugRunning = false;
        DebugOutput = null;
        ModalDebug.Open();
    }

    private async Task RunDebugAsync()
    {
        if (DebugHandler == null)
        {
            DebugOutput = ["No debug handler is registered."];
            await InvokeAsync(StateHasChanged);
            return;
        }

        DebugRunning = true;
        DebugOutput = null;
        await InvokeAsync(StateHasChanged);
        try
        {
            DebugOutput = await DebugHandler(ServiceProvider);
        }
        catch (Exception e)
        {
            DebugOutput = [$"Error: {e.Message}"];
        }
        finally
        {
            DebugRunning = false;
            await InvokeAsync(StateHasChanged);
        }
    }

	public class SidebarEntry
	{
		public string Id { get; set; }
		public string Label { get; set; }
		public int Priority { get; set; } = 100;

		public SidebarEntry(string id = "", string label = "", int priority = 100)
		{
			Id = id;
			Label = label;
			Priority = priority;
		}
	}

	public class SidebarSection : SidebarEntry
	{
		public IEnumerable<SidebarItem> Items { get; set; } = [];

		public SidebarSection(string id = "", string label = "", int priority = 100) : base(id, label, priority)
		{
		}
	}

	public class SidebarItem : SidebarEntry
	{
		public string Icon { get; set; }
		public string Url { get; set; }

		public SidebarItem(string id = "", string label = "", int priority = 100, string url = "", string icon = "") : base(id, label, priority)
		{
			Url = url;
			Icon = icon;
		}
	}

	private sealed class SidebarEntryComparer : IComparer<SidebarEntry>
	{
		public static readonly SidebarEntryComparer Instance = new();

		public int Compare(SidebarEntry? x, SidebarEntry? y)
		{
			if (x is null && y is null) return 0;
			if (x is null) return -1;
			if (y is null) return 1;
			if (string.Compare(x.Id, y.Id, StringComparison.OrdinalIgnoreCase) == 0 ) return 0;

			var priorityComparison = x.Priority.CompareTo(y.Priority);
			if (priorityComparison != 0)
			{
				return priorityComparison;
			}

			return string.Compare(x.Label, y.Label, StringComparison.OrdinalIgnoreCase);
		}
	}

	[Inject]
	protected NavigationManager NavigationManager { get; init; } = default!;

	private static readonly ISet<SidebarEntry> _sidebarMenu = new SortedSet<SidebarEntry>(SidebarEntryComparer.Instance);

	public static void AddOrReplaceEntry(SidebarEntry entry)
	{
		_sidebarMenu.Add(entry);
	}

	public static IEnumerable<SidebarEntry> Entries
	{
		get
		{
			return [.. _sidebarMenu];
		}
	}
}
