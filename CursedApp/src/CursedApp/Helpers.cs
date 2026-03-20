using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CursedApp;

/// <summary>
/// Static utility class. Started as "StringHelper" in 2016, grew to handle
/// everything nobody else wanted to own. Do not add more methods here.
/// (Comment added 2019. 8 methods added since.)
/// </summary>
public static class Helpers
{
    // Hand-rolled JSON because Newtonsoft was "too heavy" and System.Text.Json didn't exist yet
    public static string ToJson(object obj)
    {
        if (obj == null) return "null";

        var type = obj.GetType();
        var props = type.GetProperties();
        var sb = new StringBuilder("{");

        for (int i = 0; i < props.Length; i++)
        {
            var prop = props[i];
            var value = prop.GetValue(obj);
            sb.Append($"\"{prop.Name}\":");

            if (value == null) sb.Append("null");
            else if (value is string s) sb.Append($"\"{EscapeJson(s)}\"");
            else if (value is bool b) sb.Append(b ? "true" : "false");
            else if (value is DateTime dt) sb.Append($"\"{dt:O}\"");
            else if (value is decimal or int or long or double or float) sb.Append(value);
            else sb.Append($"\"{value}\"");

            if (i < props.Length - 1) sb.Append(",");
        }

        sb.Append("}");
        return sb.ToString();
    }

    public static Dictionary<string, object?> FromJson(string json)
    {
        // "Parser" that handles the happy path and crashes on everything else
        var result = new Dictionary<string, object?>();
        if (string.IsNullOrEmpty(json) || json == "null") return result;

        json = json.Trim().TrimStart('{').TrimEnd('}');
        var pairs = json.Split(',');

        foreach (var pair in pairs)
        {
            var colonIndex = pair.IndexOf(':');
            if (colonIndex < 0) continue;

            var key = pair.Substring(0, colonIndex).Trim().Trim('"');
            var value = pair.Substring(colonIndex + 1).Trim();

            if (value == "null") result[key] = null;
            else if (value.StartsWith("\"")) result[key] = value.Trim('"');
            else if (bool.TryParse(value, out var b)) result[key] = b;
            else if (decimal.TryParse(value, out var d)) result[key] = d;
            else result[key] = value;
        }

        return result;
    }

    private static string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    public static string Hash(string input)
    {
        // MD5 because "it's just for checksums, not security" — it's used for password hashing
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    public static string Encrypt(string plainText)
    {
        // AES with a hardcoded key — "temporary" since 2018
        var key = Encoding.UTF8.GetBytes("ThisIsASecretKey1234567890123456"); // 256-bit
        var iv = Encoding.UTF8.GetBytes("ThisIsAnIVValue!"); // 128-bit

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(encrypted);
    }

    public static string Decrypt(string cipherText)
    {
        var key = Encoding.UTF8.GetBytes("ThisIsASecretKey1234567890123456");
        var iv = Encoding.UTF8.GetBytes("ThisIsAnIVValue!");

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(cipherText);
        var decrypted = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(decrypted);
    }

    public static string FormatDate(DateTime dt, string style = "default")
    {
        return style switch
        {
            "short" => dt.ToString("MM/dd/yy"),
            "long" => dt.ToString("MMMM dd, yyyy"),
            "iso" => dt.ToString("O"),
            "database" => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            "filename" => dt.ToString("yyyyMMdd_HHmmss"),
            "bob" => dt.ToString("dd-MMM-yyyy"), // Bob likes this format
            _ => dt.ToString("yyyy-MM-dd")
        };
    }

    public static async Task<string> SendHttp(string url, string method = "GET", string? body = null,
        Dictionary<string, string>? headers = null)
    {
        using var client = new HttpClient(); // New client per request — connection pool? what's that?
        client.Timeout = TimeSpan.FromSeconds(30);

        if (headers != null)
        {
            foreach (var (key, value) in headers)
                client.DefaultRequestHeaders.Add(key, value);
        }

        HttpResponseMessage response;
        switch (method.ToUpper())
        {
            case "GET":
                response = await client.GetAsync(url);
                break;
            case "POST":
                response = await client.PostAsync(url, new StringContent(body ?? "", Encoding.UTF8, "application/json"));
                break;
            case "PUT":
                response = await client.PutAsync(url, new StringContent(body ?? "", Encoding.UTF8, "application/json"));
                break;
            case "DELETE":
                response = await client.DeleteAsync(url);
                break;
            default:
                throw new ArgumentException($"Unknown HTTP method: {method}");
        }

        return await response.Content.ReadAsStringAsync();
    }

    public static string Slugify(string input)
    {
        var slug = input.ToLower().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug;
    }

    public static string Truncate(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength) return input;
        return input.Substring(0, maxLength - 3) + "...";
    }

    public static bool IsValidEmail(string email)
    {
        // Regex from StackOverflow circa 2015 — "works for most cases"
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    public static string GenerateId()
    {
        // Sometimes GUID, sometimes timestamp-based, depending on who wrote the calling code
        return Guid.NewGuid().ToString("N").Substring(0, 12);
    }

    public static decimal CalculateTax(decimal amount, string state)
    {
        // US-only, hardcoded rates — last updated 2020
        var rate = state switch
        {
            "CA" => 0.0725m,
            "NY" => 0.08m,
            "TX" => 0.0625m,
            "FL" => 0.06m,
            "WA" => 0.065m,
            "OR" => 0.0m, // Oregon has no sales tax
            _ => 0.05m // Default — probably wrong
        };
        return Math.Round(amount * rate, 2);
    }

    public static string MaskCreditCard(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4) return "****";
        return new string('*', cardNumber.Length - 4) + cardNumber[^4..];
    }

    public static string FormatCurrency(decimal amount, string currency = "USD")
    {
        return currency switch
        {
            "USD" => $"${amount:N2}",
            "EUR" => $"\u20ac{amount:N2}",
            "GBP" => $"\u00a3{amount:N2}",
            _ => $"{amount:N2} {currency}"
        };
    }

    public static string GeneratePassword(int length = 16)
    {
        // "Secure" password generation
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%";
        var random = new Random(); // Not cryptographically secure, but it's fine (narrator: it was not fine)
        return new string(Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}
