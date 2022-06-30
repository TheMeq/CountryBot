using System;

namespace DiscordBotTemplate.Utilities;

public static class GeneralUtility
{
    public static string ToReadableString(TimeSpan span)
    {
        var formatted =
            $"{(span.Duration().Days > 0 ? $"{span.Days:n0} day{span.Days.Plural()}, " : string.Empty)}" +
            $"{(span.Duration().Hours > 0 ? $"{span.Hours:0} hour{span.Hours.Plural()}, " : string.Empty)}" +
            $"{(span.Duration().Minutes > 0 ? $"{span.Minutes:0} minute{span.Minutes.Plural()}, " : string.Empty)}" +
            $"{(span.Duration().Seconds > 0 ? $"{span.Seconds:0} second{span.Seconds.Plural()}" : string.Empty)}";
        if (formatted.EndsWith(", ")) formatted = formatted[..^2];
        if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";
        return formatted;
    }

    private static string Plural(this int input)
    {
        return input != 1 ? "s" : string.Empty;
    }

    public static ulong ToTimeStamp(this DateTime dateTime)
    {
        return (ulong)((DateTimeOffset)dateTime).ToUnixTimeSeconds();
    }
    
    public static DateTime ToDateTime(this double timeStamp)
    {
        return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timeStamp).ToUniversalTime();
    }

}