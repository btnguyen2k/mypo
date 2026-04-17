namespace MyPo.Libs.Tempus;

public static class DateTimeOffsetExtensions
{
	/// <summary>
	/// Returns a new DateTimeOffset with the time component set to 00:00:00
	/// </summary>
	/// <param name="dateTime"></param>
	/// <returns></returns>
	public static DateTimeOffset StartOfDay(this DateTimeOffset dateTime)
	{
		return new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, 0, 0, dateTime.Offset);
	}

	/// <summary>
	/// Returns a new DateTimeOffset representing the previous weekday (Monday to Friday) of the given DateTimeOffset.
	/// </summary>
	/// <param name="dateTime"></param>
	/// <returns></returns>
	public static DateTimeOffset PrevWeekDay(this DateTimeOffset dateTime)
	{
		var prevDay = dateTime.AddDays(-1);
		while (prevDay.DayOfWeek == DayOfWeek.Saturday || prevDay.DayOfWeek == DayOfWeek.Sunday)
		{
			prevDay = prevDay.AddDays(-1);
		}
		return prevDay;
	}

	/// <summary>
	/// Returns a new DateTimeOffset representing the next weekday (Monday to Friday) of the given DateTimeOffset.
	/// </summary>
	/// <param name="dateTime"></param>
	/// <returns></returns>
	public static DateTimeOffset NextWeekDay(this DateTimeOffset dateTime)
	{
		var nextDay = dateTime.AddDays(1);
		while (nextDay.DayOfWeek == DayOfWeek.Saturday || nextDay.DayOfWeek == DayOfWeek.Sunday)
		{
			nextDay = nextDay.AddDays(1);
		}
		return nextDay;
	}

	/// <summary>
	/// Checks if the time component of the given DateTimeOffset falls within the specified time window (e.g. start <= dateTime < end).
	/// </summary>
	/// <param name="dateTime"></param>
	/// <param name="start"></param>
	/// <param name="end"></param>
	/// <returns></returns>
	/// <remarks>The time window can span across midnight. For example, a time window from 22:00 to 02:00 will include times from 22:00 to 23:59 and from 00:00 to 02:00.</remarks>
	public static bool WithinTimeWindow(this DateTimeOffset dateTime, TimeOnly start, TimeOnly end)
	{
		var time = TimeOnly.FromDateTime(dateTime.DateTime);
		return start <= end
			? time >= start && time < end
			: time >= start || time < end;
	}

	/// <summary>
	/// Checks if the day of week component of the given DateTimeOffset falls within the specified list of days of week.
	/// </summary>
	/// <param name="dateTime"></param>
	/// <param name="dows"></param>
	/// <returns></returns>
	public static bool WithinDowList(this DateTimeOffset dateTime, params DayOfWeek[] dows)
	{
		return dows.Contains(dateTime.DayOfWeek);
	}

	/// <summary>
	/// Checks if the day of week component of the given DateTimeOffset falls within the specified list of days of week.
	/// </summary>
	/// <param name="dateTime"></param>
	/// <param name="dows"></param>
	/// <returns></returns>
	public static bool WithinDowList(this DateTimeOffset dateTime, IEnumerable<DayOfWeek> dows)
	{
		return dows.Contains(dateTime.DayOfWeek);
	}

	/// <summary>
	/// Checks if the day of week component of the given DateTimeOffset falls within the specified list of days of week (case-insensitive, supports both full and abbreviated day of week names).
	/// </summary>
	/// <param name="dateTime"></param>
	/// <param name="dows"></param>
	/// <returns></returns>
	public static bool WithinDowList(this DateTimeOffset dateTime, params string[] dows)
	{
		var dowStr1 = dateTime.DayOfWeek.ToString().ToUpper();
		var dowStr2 = dowStr1.Substring(0, 3);
		return dows.Where(d => d.Equals(dowStr1, StringComparison.OrdinalIgnoreCase) || d.Equals(dowStr2, StringComparison.OrdinalIgnoreCase)).Any();
	}

	/// <summary>
	/// Checks if the day of week component of the given DateTimeOffset falls within the specified list of days of week (case-insensitive, supports both full and abbreviated day of week names).
	/// </summary>
	/// <param name="dateTime"></param>
	/// <param name="dows"></param>
	/// <returns></returns>
	public static bool WithinDowList(this DateTimeOffset dateTime, IEnumerable<string> dows)
	{
		var dowStr1 = dateTime.DayOfWeek.ToString().ToUpper();
		var dowStr2 = dowStr1.Substring(0, 3);
		return dows.Where(d => d.Equals(dowStr1, StringComparison.OrdinalIgnoreCase) || d.Equals(dowStr2, StringComparison.OrdinalIgnoreCase)).Any();
	}

	/// <summary>
	/// Converts the given DateTimeOffset to the specified time zone. Returns null if the time zone ID is invalid or not found.
	/// </summary>
	/// <param name="dateTime"></param>
	/// <param name="tzId"></param>
	/// <returns></returns>
	public static DateTimeOffset? ToTimeZoneSilently(this DateTimeOffset dateTime, string tzId)
	{
		try
		{
			var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
			return TimeZoneInfo.ConvertTime(dateTime, timeZone);
		}
		catch (Exception e) when (e is TimeZoneNotFoundException || e is InvalidTimeZoneException)
		{
			return null;
		}
	}

	/// <summary>
	/// Shifts the given DateTimeOffset to the specified time zone (e.g. keep date and time, change only the time zone). Returns null if the time zone ID is invalid or not found.
	/// </summary>
	/// <param name="dateTime"></param>
	/// <param name="tzId"></param>
	/// <returns></returns>
	public static DateTimeOffset? AsTimeZoneSilently(this DateTimeOffset dateTime, string tzId)
	{
		try
		{
			var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
			return new DateTimeOffset(dateTime.DateTime, timeZone.GetUtcOffset(dateTime));
		}
		catch (Exception e) when (e is TimeZoneNotFoundException || e is InvalidTimeZoneException)
		{
			return null;
		}
	}
}
