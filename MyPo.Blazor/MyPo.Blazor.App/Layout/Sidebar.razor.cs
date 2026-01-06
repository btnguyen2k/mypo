using Microsoft.AspNetCore.Components;

namespace MyPo.Blazor.App.Layout;

public partial class Sidebar
{
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
