using System;
using System.Data;
using System.IO;
using MySql.Data.MySqlClient;

namespace InventorySystem
{
    public static class DbHelper
    {
        private const string DefaultConnectionString =
            "Server=localhost;Port=3306;Database=inventory_db;Uid=root;Pwd=;Connection Timeout=5;";

        private static string connectionString;

        /// <summary>
        /// Reads the connection string from "connection.txt" next to the .exe
        /// (so you don't have to recompile to change the MySQL password).
        /// Falls back to the default above if the file is missing or empty.
        /// </summary>
        public static MySqlConnection GetConnection()
        {
            if (connectionString == null)
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "connection.txt");
                string text = File.Exists(path) ? File.ReadAllText(path).Trim() : "";
                connectionString = text.Length > 0 ? text : DefaultConnectionString;
            }
            return new MySqlConnection(connectionString);
        }

        // ---------- small helpers so pages stay short ----------
        // Parameters are passed as name, value pairs:  Query(sql, "@id", 5, "@name", "x")

        public static DataTable Query(string sql, params object[] args)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = MakeCommand(sql, conn, args))
                using (var adapter = new MySqlDataAdapter(cmd))
                {
                    var table = new DataTable();
                    adapter.Fill(table);
                    return table;
                }
            }
        }

        public static object Scalar(string sql, params object[] args)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = MakeCommand(sql, conn, args))
                    return cmd.ExecuteScalar();
            }
        }

        public static int Exec(string sql, params object[] args)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = MakeCommand(sql, conn, args))
                    return cmd.ExecuteNonQuery();
            }
        }

        private static MySqlCommand MakeCommand(string sql, MySqlConnection conn, object[] args)
        {
            var cmd = new MySqlCommand(sql, conn);
            for (int i = 0; i + 1 < args.Length; i += 2)
                cmd.Parameters.AddWithValue((string)args[i], args[i + 1] ?? DBNull.Value);
            return cmd;
        }

        /// <summary>Builds a safe LIKE pattern: "%text%" with % and _ escaped.</summary>
        public static string Like(string text)
        {
            text = (text ?? "").Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            return "%" + text + "%";
        }

        /// <summary>Empty/blank text becomes SQL NULL, otherwise the trimmed text.</summary>
        public static object NullIfEmpty(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? (object)DBNull.Value : text.Trim();
        }

        /// <summary>Turns a MySQL error into a short message that is safe to show to a user.</summary>
        public static string Describe(MySqlException ex)
        {
            switch (ex.Number)
            {
                case 0:
                case 1042: return "Cannot reach the MySQL server. Make sure MySQL is running.";
                case 1045: return "MySQL rejected the login. Check Uid/Pwd in connection.txt.";
                case 1049: return "Database 'inventory_db' was not found. Run schema.sql first.";
                case 1146: return "A table is missing. Run schema.sql (and upgrade_existing.sql if you had the old version).";
                case 1054: return "The users table is out of date. Run upgrade_existing.sql.";
                case 1451: return "This record is used by other data and can't be deleted.";
                case 1452: return "The selected supplier or product no longer exists.";
                case 3819:
                case 4025: return "A value is outside the allowed range.";
                default:   return "A database error occurred (code " + ex.Number + ").";
            }
        }
    }
}
