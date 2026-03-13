namespace MyPo.Libs.Tempus;

public static class DateTimeExtensions
{
	/// <summary>
	/// Returns a new DateTime with the time component set to 00:00:00
	/// </summary>
	/// <param name="dateTime"></param>
	/// <returns></returns>
	public static DateTime StartOfDay(this DateTime dateTime)
	{
		return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, 0, 0, dateTime.Kind);
	}

	/// <summary>
	/// Returns a new DateTime representing the previous weekday (Monday to Friday) of the given DateTime.
	/// </summary>
	/// <param name="dateTime"></param>
	/// <returns></returns>
	public static DateTime PrevWeekDay(this DateTime dateTime)
	{
		var prevDay = dateTime.AddDays(-1);
		while (prevDay.DayOfWeek == DayOfWeek.Saturday || prevDay.DayOfWeek == DayOfWeek.Sunday)
		{
			prevDay = prevDay.AddDays(-1);
		}
		return prevDay;
	}

	/// <summary>
	/// Returns a new DateTime representing the next weekday (Monday to Friday) of the given DateTime.
	/// </summary>
	/// <param name="dateTime"></param>
	/// <returns></returns>
	public static DateTime NextWeekDay(this DateTime dateTime)
	{
		var nextDay = dateTime.AddDays(1);
		while (nextDay.DayOfWeek == DayOfWeek.Saturday || nextDay.DayOfWeek == DayOfWeek.Sunday)
		{
			nextDay = nextDay.AddDays(1);
		}
		return nextDay;
	}

	/// <summary>
	/// Checks if the time component of the given DateTime falls within the specified time window (e.g. start <= dateTime < end).
	/// </summary>
	/// <param name="dateTime"></param>
	/// <param name="start"></param>
	/// <param name="end"></param>
	/// <returns></returns>
	/// <remarks>The time window can span across midnight. For example, a time window from 22:00 to 02:00 will include times from 22:00 to 23:59 and from 00:00 to 02:00.</remarks>
	public static bool WithinTimeWindow(this DateTime dateTime, TimeOnly start, TimeOnly end)
	{
		var time = TimeOnly.FromDateTime(dateTime);
		if (start <= end)
		{
			return time >= start && time < end;
		}
		else
		{
			return time >= start || time < end;
		}
	}

	/// <summary>
	/// Checks if the day of week component of the given DateTime falls within the specified list of days of week.
	/// </summary>
	/// <param name="dateTime"></param>
	/// <param name="dows"></param>
	/// <returns></returns>
	public static bool WithinDowList(this DateTime dateTime, params DayOfWeek[] dows)
	{
		return dows.Contains(dateTime.DayOfWeek);
	}

	/// <summary>
	/// Checks if the day of week component of the given DateTime falls within the specified list of days of week.
	/// </summary>
	/// <param name="dateTime"></param>
	/// <param name="dows"></param>
	/// <returns></returns>
	public static bool WithinDowList(this DateTime dateTime, IEnumerable<DayOfWeek> dows)
	{
		return dows.Contains(dateTime.DayOfWeek);
	}

	/// <summary>
	/// Checks if the day of week component of the given DateTime falls within the specified list of days of week (case-insensitive, supports both full and abbreviated day of week names).
	/// </summary>
	/// <param name="dateTime"></param>
	/// <param name="dows"></param>
	/// <returns></returns>
	public static bool WithinDowList(this DateTime dateTime, params string[] dows)
	{
		var dowStr1 = dateTime.DayOfWeek.ToString().ToUpper();
		var dowStr2 = dowStr1.Substring(0, 3);
		return dows.Where(d => d.Equals(dowStr1, StringComparison.OrdinalIgnoreCase) || d.Equals(dowStr2, StringComparison.OrdinalIgnoreCase)).Any();
	}

	/// <summary>
	/// Checks if the day of week component of the given DateTime falls within the specified list of days of week (case-insensitive, supports both full and abbreviated day of week names).
	/// </summary>
	/// <param name="dateTime"></param>
	/// <param name="dows"></param>
	/// <returns></returns>
	public static bool WithinDowList(this DateTime dateTime, IEnumerable<string> dows)
	{
		var dowStr1 = dateTime.DayOfWeek.ToString().ToUpper();
		var dowStr2 = dowStr1.Substring(0, 3);
		return dows.Where(d => d.Equals(dowStr1, StringComparison.OrdinalIgnoreCase) || d.Equals(dowStr2, StringComparison.OrdinalIgnoreCase)).Any();
	}
}
