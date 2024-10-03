using System;

namespace fs24bot3.Core;

public static class TimeSpanExtensions
{
    public static string ToReadableString(this TimeSpan span)
    {
        string formatted = string.Format("{0}{1}{2}{3}",
            span.Duration().Days > 0 ? string.Format("{0:0} дн. ", span.Days) : string.Empty,
            span.Duration().Hours > 0 ? string.Format("{0:0} ч. ", span.Hours) : string.Empty,
            span.Duration().Minutes > 0 ? string.Format("{0:0} мин. ", span.Minutes) : string.Empty,
            span.Duration().Seconds > 0 ? string.Format("{0:0} сек.", span.Seconds) : string.Empty);
        if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

        if (string.IsNullOrEmpty(formatted)) formatted = "0 секунд";

        return formatted;
    }
}