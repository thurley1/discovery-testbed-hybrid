using System;
using System.Collections.Generic;
using System.Text;

namespace CursedApp.Lib;

/// <summary>
/// Hand-rolled JSON parser from before System.Text.Json existed.
/// Also before Newtonsoft was added. Also after Newtonsoft was removed
/// because of "dependency bloat." Also exists alongside Helpers.ToJson()
/// which does the same thing differently.
///
/// Handles: objects, arrays, strings, numbers, booleans, null.
/// Does not handle: nested objects, escaped quotes, Unicode, or reality.
/// </summary>
public static class JsonHelper
{
    public static string Serialize(Dictionary<string, object?> obj)
    {
        var sb = new StringBuilder("{");
        var first = true;

        foreach (var (key, value) in obj)
        {
            if (!first) sb.Append(',');
            first = false;

            sb.Append($"\"{key}\":");
            sb.Append(SerializeValue(value));
        }

        sb.Append('}');
        return sb.ToString();
    }

    public static string SerializeArray(List<Dictionary<string, object?>> items)
    {
        var sb = new StringBuilder("[");

        for (int i = 0; i < items.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(Serialize(items[i]));
        }

        sb.Append(']');
        return sb.ToString();
    }

    private static string SerializeValue(object? value)
    {
        if (value == null) return "null";
        if (value is string s) return $"\"{EscapeString(s)}\"";
        if (value is bool b) return b ? "true" : "false";
        if (value is DateTime dt) return $"\"{dt:O}\"";
        if (value is int or long or decimal or double or float) return value.ToString() ?? "0";
        if (value is Dictionary<string, object?> dict) return Serialize(dict);
        if (value is List<Dictionary<string, object?>> list) return SerializeArray(list);
        return $"\"{value}\""; // Fallback — just toString it
    }

    private static string EscapeString(string s)
    {
        return s
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    // "Deserialize" — it's more of a best-effort guess
    public static Dictionary<string, string> Deserialize(string json)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(json)) return result;

        // Strip outer braces
        json = json.Trim();
        if (json.StartsWith('{')) json = json[1..];
        if (json.EndsWith('}')) json = json[..^1];

        // Split on commas (breaks if values contain commas, but "that never happens")
        var pairs = json.Split(',');
        foreach (var pair in pairs)
        {
            var colonIdx = pair.IndexOf(':');
            if (colonIdx < 0) continue;

            var key = pair[..colonIdx].Trim().Trim('"');
            var value = pair[(colonIdx + 1)..].Trim().Trim('"');

            result[key] = value;
        }

        return result;
    }

    // Added because someone needed to check if a string was JSON
    public static bool IsJson(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        input = input.Trim();
        return (input.StartsWith('{') && input.EndsWith('}')) ||
               (input.StartsWith('[') && input.EndsWith(']'));
    }

    // Pretty print — because the logs were unreadable
    public static string PrettyPrint(string json, int indent = 2)
    {
        var sb = new StringBuilder();
        var level = 0;
        var inString = false;

        foreach (var c in json)
        {
            if (c == '"' && (sb.Length == 0 || sb[^1] != '\\'))
            {
                inString = !inString;
                sb.Append(c);
                continue;
            }

            if (inString)
            {
                sb.Append(c);
                continue;
            }

            switch (c)
            {
                case '{':
                case '[':
                    sb.Append(c);
                    sb.AppendLine();
                    level++;
                    sb.Append(new string(' ', level * indent));
                    break;
                case '}':
                case ']':
                    sb.AppendLine();
                    level--;
                    sb.Append(new string(' ', level * indent));
                    sb.Append(c);
                    break;
                case ',':
                    sb.Append(c);
                    sb.AppendLine();
                    sb.Append(new string(' ', level * indent));
                    break;
                case ':':
                    sb.Append(": ");
                    break;
                default:
                    if (!char.IsWhiteSpace(c))
                        sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }
}
