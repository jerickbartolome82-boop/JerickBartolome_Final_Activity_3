using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace InventorySystem
{
    /// <summary>
    /// Salted PBKDF2-SHA256. Stored format: "iterations.saltBase64.hashBase64".
    /// The iteration count is stored with the hash, so old hashes keep working
    /// and can be upgraded automatically (see NeedsRehash).
    /// </summary>
    public static class PasswordHasher
    {
        private const int Iterations = 600000;
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int MaxAcceptedIterations = 5000000;   // guards against a tampered row hanging the app

        // Valid-looking hash that matches no password. Used so that an unknown
        // username costs the same time as a wrong password (no user enumeration by timing).
        private const string DummyHash =
            "600000.AAAAAAAAAAAAAAAAAAAAAA==.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

        public static string Hash(string password)
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            byte[] hash = Derive(password, salt, Iterations);
            return Iterations + "." + Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
        }

        public static bool Verify(string password, string stored)
        {
            try
            {
                string[] parts = (stored ?? "").Split('.');
                if (parts.Length != 3) return false;

                int iterations;
                if (!int.TryParse(parts[0], out iterations) || iterations < 1 || iterations > MaxAcceptedIterations)
                    return false;

                byte[] salt = Convert.FromBase64String(parts[1]);
                byte[] expected = Convert.FromBase64String(parts[2]);
                byte[] actual = Derive(password, salt, iterations);
                return FixedTimeEquals(actual, expected);
            }
            catch (FormatException) { return false; }
        }

        public static void VerifyDummy(string password)
        {
            Verify(password, DummyHash);
        }

        public static bool NeedsRehash(string stored)
        {
            string[] parts = (stored ?? "").Split('.');
            int iterations;
            return parts.Length != 3 || !int.TryParse(parts[0], out iterations) || iterations < Iterations;
        }

        private static byte[] Derive(string password, byte[] salt, int iterations)
        {
            using (var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                return kdf.GetBytes(KeySize);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
