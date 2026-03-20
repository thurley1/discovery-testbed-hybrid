using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CursedApp.Shared;

/// <summary>
/// Extension methods on EVERYTHING. Because extension methods are "basically free"
/// and "make the code read more naturally." Added by various team members over 4 years.
/// Nobody knows which ones are actually used.
/// </summary>
public static class Extensions
{
    // === Object extensions ===

    public static string ToSafeString(this object? obj)
    {
        return obj?.ToString() ?? "";
    }

    public static bool IsNull(this object? obj)
    {
        return obj == null;
    }

    public static bool IsNotNull(this object? obj)
    {
        return obj != null;
    }

    public static T OrDefault<T>(this T? obj, T defaultValue) where T : class
    {
        return obj ?? defaultValue;
    }

    // === String extensions ===

    public static bool IsNullOrEmpty(this string? s)
    {
        return string.IsNullOrEmpty(s);
    }

    public static bool HasValue(this string? s)
    {
        return !string.IsNullOrWhiteSpace(s);
    }

    public static string ToCamelCase(this string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToLower(s[0]) + s[1..];
    }

    public static string ToPascalCase(this string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s[1..];
    }

    public static string ToSnakeCase(this string s)
    {
        return Regex.Replace(s, @"([A-Z])", "_$1").TrimStart('_').ToLower();
    }

    public static string ToKebabCase(this string s)
    {
        return Regex.Replace(s, @"([A-Z])", "-$1").TrimStart('-').ToLower();
    }

    public static string Repeat(this string s, int count)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++) sb.Append(s);
        return sb.ToString();
    }

    public static string RemoveWhitespace(this string s)
    {
        return Regex.Replace(s, @"\s+", "");
    }

    public static string Left(this string s, int length)
    {
        return s.Length <= length ? s : s[..length];
    }

    public static string Right(this string s, int length)
    {
        return s.Length <= length ? s : s[^length..];
    }

    // === Int extensions (why?) ===

    public static bool IsBetween(this int value, int min, int max)
    {
        return value >= min && value <= max;
    }

    public static string ToOrdinal(this int num)
    {
        var suffix = (num % 100) switch
        {
            11 or 12 or 13 => "th",
            _ => (num % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            }
        };
        return $"{num}{suffix}";
    }

    public static bool IsEven(this int value) => value % 2 == 0;
    public static bool IsOdd(this int value) => value % 2 != 0;

    // === DateTime extensions ===

    public static bool IsWeekend(this DateTime dt)
    {
        return dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday;
    }

    public static bool IsBusinessDay(this DateTime dt)
    {
        return !dt.IsWeekend(); // Doesn't account for holidays — "we'll add that later"
    }

    public static DateTime StartOfDay(this DateTime dt) => dt.Date;
    public static DateTime EndOfDay(this DateTime dt) => dt.Date.AddDays(1).AddTicks(-1);
    public static DateTime StartOfMonth(this DateTime dt) => new DateTime(dt.Year, dt.Month, 1);
    public static DateTime EndOfMonth(this DateTime dt) => dt.StartOfMonth().AddMonths(1).AddTicks(-1);

    public static string TimeAgo(this DateTime dt)
    {
        var span = DateTime.UtcNow - dt;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalDays < 1) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 30) return $"{(int)span.TotalDays}d ago";
        return dt.ToString("MMM dd, yyyy");
    }

    // === Collection extensions ===

    public static bool IsEmpty<T>(this IEnumerable<T> source)
    {
        return !source.Any();
    }

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
    {
        return source.Where(x => x != null)!;
    }

    public static string JoinWith<T>(this IEnumerable<T> source, string separator)
    {
        return string.Join(separator, source);
    }

    public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize)
    {
        var list = source.ToList();
        for (int i = 0; i < list.Count; i += chunkSize)
        {
            yield return list.Skip(i).Take(chunkSize);
        }
    }

    // === Dictionary extensions ===

    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> factory)
        where TKey : notnull
    {
        if (!dict.TryGetValue(key, out var value))
        {
            value = factory();
            dict[key] = value;
        }
        return value;
    }

    public static TValue? GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue? defaultValue = default)
        where TKey : notnull
    {
        return dict.TryGetValue(key, out var value) ? value : defaultValue;
    }
}
