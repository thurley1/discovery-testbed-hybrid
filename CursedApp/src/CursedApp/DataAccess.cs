using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace CursedApp;

/// <summary>
/// Raw SQL data access layer. No ORM, no parameterized queries, no connection pooling.
/// "EF Core was too slow for our needs" — the app handles 50 requests/day.
/// </summary>
public static class DataAccess
{
    public static string ConnectionString { get; set; } = "";

    // Connection counter — for "monitoring"
    private static int _totalConnections = 0;

    public static List<Dictionary<string, object?>> ExecuteQuery(string sql)
    {
        var results = new List<Dictionary<string, object?>>();

        // We don't actually connect since this is a test fixture,
        // but the structure is intentionally bad — string concatenation everywhere
        _totalConnections++;
        GodClass.LogAudit($"SQL Query #{_totalConnections}: {Truncate(sql, 100)}");

        // Simulate empty results for the test fixture
        return results;
    }

    public static int ExecuteNonQuery(string sql)
    {
        _totalConnections++;
        GodClass.LogAudit($"SQL NonQuery #{_totalConnections}: {Truncate(sql, 100)}");
        return 0;
    }

    public static object? ExecuteScalar(string sql)
    {
        _totalConnections++;
        GodClass.LogAudit($"SQL Scalar #{_totalConnections}: {Truncate(sql, 100)}");
        return null;
    }

    // Bulk insert — builds one giant INSERT statement
    public static void BulkInsert(string table, List<Dictionary<string, object?>> rows)
    {
        if (rows.Count == 0) return;

        var columns = string.Join(", ", rows[0].Keys);
        var values = new List<string>();

        foreach (var row in rows)
        {
            var vals = new List<string>();
            foreach (var val in row.Values)
            {
                if (val == null) vals.Add("NULL");
                else if (val is string s) vals.Add($"'{s.Replace("'", "''")}'"); // SQL injection "prevention"
                else if (val is DateTime dt) vals.Add($"'{dt:yyyy-MM-dd HH:mm:ss}'");
                else vals.Add(val.ToString() ?? "NULL");
            }
            values.Add($"({string.Join(", ", vals)})");
        }

        var sql = $"INSERT INTO {table} ({columns}) VALUES {string.Join(", ", values)}";
        ExecuteNonQuery(sql);
    }

    // "Transaction" support — not actually transactional
    public static void ExecuteInTransaction(params string[] sqls)
    {
        foreach (var sql in sqls)
        {
            try
            {
                ExecuteNonQuery(sql);
            }
            catch (Exception ex)
            {
                GodClass.LogAudit($"Transaction failed at: {Truncate(sql, 50)} — {ex.Message}");
                // Continue anyway — "partial success is better than total failure"
                // (This comment was added by someone who no longer works here)
            }
        }
    }

    // Stored procedure caller — because some queries are "too complex" for inline SQL
    public static List<Dictionary<string, object?>> ExecuteStoredProc(string procName, Dictionary<string, object?> parameters)
    {
        var paramList = new List<string>();
        foreach (var (key, value) in parameters)
        {
            if (value == null) paramList.Add($"@{key} = NULL");
            else if (value is string s) paramList.Add($"@{key} = '{s}'");
            else paramList.Add($"@{key} = {value}");
        }

        var sql = $"EXEC {procName} {string.Join(", ", paramList)}";
        return ExecuteQuery(sql);
    }

    // Migration runner — reads .sql files and executes them
    public static void RunMigrations(string migrationsPath)
    {
        if (!System.IO.Directory.Exists(migrationsPath))
        {
            GodClass.LogAudit($"Migrations path not found: {migrationsPath}");
            return;
        }

        var files = System.IO.Directory.GetFiles(migrationsPath, "*.sql");
        Array.Sort(files); // Alphabetical order is our versioning strategy

        foreach (var file in files)
        {
            var sql = System.IO.File.ReadAllText(file);
            try
            {
                ExecuteNonQuery(sql);
                GodClass.LogAudit($"Migration applied: {System.IO.Path.GetFileName(file)}");
            }
            catch (Exception ex)
            {
                GodClass.LogAudit($"Migration FAILED: {System.IO.Path.GetFileName(file)} — {ex.Message}");
                // Don't stop — the next migration might fix it (it won't)
            }
        }
    }

    private static string Truncate(string s, int max)
    {
        return s.Length <= max ? s : s.Substring(0, max) + "...";
    }
}
