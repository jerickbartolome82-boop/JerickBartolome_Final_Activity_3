using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace InventorySystem
{
    /// <summary>
    /// All input rules in one place. Each method returns an error message,
    /// or null when the value is valid.
    /// </summary>
    internal static class Validators
    {
        public const int NameMax = 100;
        public const int UserMin = 4, UserMax = 30;
        public const int EmailMax = 100;
        public const int PassMin = 8, PassMax = 64;

        private static readonly Regex NameRx  = new Regex(@"^\p{L}[\p{L} .'\-]*$");
        private static readonly Regex UserRx  = new Regex(@"^[A-Za-z0-9_]+$");
        private static readonly Regex EmailRx = new Regex(@"^[A-Za-z0-9._%+\-]+@(?:[A-Za-z0-9\-]+\.)+[A-Za-z]{2,}$");

        public static string FullName(string s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return "Full name is required";
            if (s.Length < 2) return "Enter at least 2 characters";
            if (s.Length > NameMax) return "Too long (max " + NameMax + " characters)";
            if (!NameRx.IsMatch(s)) return "Use letters, spaces, . ' and - only";
            return null;
        }

        public static string Username(string s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return "Username is required";
            if (s.Length < UserMin) return "At least " + UserMin + " characters";
            if (s.Length > UserMax) return "Too long (max " + UserMax + " characters)";
            if (!char.IsLetter(s[0]) || s[0] > 127) return "Must start with a letter";
            if (!UserRx.IsMatch(s)) return "Letters, numbers and _ only";
            return null;
        }

        public static string Email(string s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return "Email is required";
            if (s.Length > EmailMax) return "Too long (max " + EmailMax + " characters)";
            if (!EmailRx.IsMatch(s) || s.Contains("..") || s.StartsWith(".") || s.Contains(".@"))
                return "Enter a valid email (name@example.com)";
            return null;
        }

        public static string Password(string p, string username)
        {
            p = p ?? "";
            if (p.Length == 0) return "Password is required";
            if (p.Length < PassMin) return "At least " + PassMin + " characters";
            if (p.Length > PassMax) return "Too long (max " + PassMax + " characters)";
            if (HasWhitespace(p)) return "Spaces are not allowed";
            if (!HasUpper(p)) return "Add an uppercase letter (A-Z)";
            if (!HasLower(p)) return "Add a lowercase letter (a-z)";
            if (!HasDigit(p)) return "Add a number (0-9)";
            username = (username ?? "").Trim();
            if (username.Length >= UserMin && p.IndexOf(username, StringComparison.OrdinalIgnoreCase) >= 0)
                return "Password can't contain your username";
            return null;
        }

        public static string Confirm(string confirm, string password)
        {
            if (string.IsNullOrEmpty(confirm)) return "Please confirm your password";
            if (confirm != password) return "Passwords do not match";
            return null;
        }

        public static string Required(string s, string what)
        {
            return string.IsNullOrWhiteSpace(s) ? what + " is required" : null;
        }

        /// <summary>Returns 0 (empty) to 4 (strong) and a label for the strength meter.</summary>
        public static int PasswordScore(string p, out string label)
        {
            label = "";
            if (string.IsNullOrEmpty(p)) return 0;

            int classes = (HasLower(p) ? 1 : 0) + (HasUpper(p) ? 1 : 0) + (HasDigit(p) ? 1 : 0) + (HasSymbol(p) ? 1 : 0);
            int pts = 0;
            if (p.Length >= 8) pts++;
            if (p.Length >= 12) pts++;
            if (classes >= 3) pts++;
            if (classes == 4 && p.Length >= 10) pts++;
            if (Password(p, "") != null) pts = Math.Min(pts, 1);   // fails the minimum rules => weak
            if (pts < 1) pts = 1;

            label = new[] { "", "Weak", "Fair", "Good", "Strong" }[pts];
            return pts;
        }

        // ---------- inventory rules ----------

        private static readonly Regex PhoneRx = new Regex(@"^[0-9+\-() ]+$");

        /// <summary>Generic text rule: trimmed length between min and max (blank allowed when not required).</summary>
        public static string Text(string s, string what, int min, int max, bool required)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return required ? what + " is required" : null;
            if (s.Length < min) return what + " must be at least " + min + " characters";
            if (s.Length > max) return what + " is too long (max " + max + ")";
            return null;
        }

        public static string Phone(string s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return null;                       // optional
            if (!PhoneRx.IsMatch(s)) return "Digits, spaces and + - ( ) only";
            int digits = 0;
            foreach (char c in s) if (c >= '0' && c <= '9') digits++;
            if (digits < 7 || digits > 15) return "Enter 7 to 15 digits";
            return null;
        }

        public static string OptionalEmail(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : Email(s);
        }

        public static string OptionalPerson(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : FullName(s);
        }

        /// <summary>Required whole number (digits only) between min and max.</summary>
        public static string IntRange(string s, string what, int min, int max)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return what + " is required";
            int v;
            if (!int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out v))
                return "Enter a whole number (digits only)";
            if (v < min || v > max) return "Enter a value from " + min.ToString("N0", CultureInfo.InvariantCulture) + " to " + max.ToString("N0", CultureInfo.InvariantCulture);
            return null;
        }

        public static int ParseInt(string s)
        {
            return int.Parse(s.Trim(), NumberStyles.None, CultureInfo.InvariantCulture);
        }

        public static string Price(string s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return "Unit price is required";
            decimal v;
            if (!decimal.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out v))
                return "Enter a valid amount (e.g. 1250.50)";
            int dot = s.IndexOf('.');
            if (dot >= 0 && s.Length - dot - 1 > 2) return "Maximum 2 decimal places";
            if (v < 0.01m || v > 9999999.99m) return "Must be between 0.01 and 9,999,999.99";
            return null;
        }

        public static decimal ParsePrice(string s)
        {
            return decimal.Parse(s.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        }

        private static bool HasUpper(string s)  { foreach (char c in s) if (c >= 'A' && c <= 'Z') return true; return false; }
        private static bool HasLower(string s)  { foreach (char c in s) if (c >= 'a' && c <= 'z') return true; return false; }
        private static bool HasDigit(string s)  { foreach (char c in s) if (c >= '0' && c <= '9') return true; return false; }
        private static bool HasSymbol(string s) { foreach (char c in s) if (!char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)) return true; return false; }
        private static bool HasWhitespace(string s) { foreach (char c in s) if (char.IsWhiteSpace(c)) return true; return false; }
    }
}
