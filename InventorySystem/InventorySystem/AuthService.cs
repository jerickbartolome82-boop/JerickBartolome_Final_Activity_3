using System;
using MySql.Data.MySqlClient;

namespace InventorySystem
{
    public enum LoginStatus { Success, InvalidCredentials, Locked, Disabled, Error }

    public class LoginResult
    {
        public LoginStatus Status;
        public string Username;
        public string Role;
        public int LockSecondsRemaining;
        public string Message;
    }

    public enum RegisterStatus { Success, UsernameTaken, EmailTaken, Error }

    public class RegisterResult
    {
        public RegisterStatus Status;
        public string Message;
    }

    /// <summary>
    /// All database work for sign in / register. These methods are synchronous
    /// and never throw; the forms call them with Task.Run so the window never freezes.
    /// </summary>
    public static class AuthService
    {
        private const int MaxFailedAttempts = 5;
        private const int LockMinutes = 5;

        public static LoginResult Login(string username, string password)
        {
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    bool found = false, active = true, locked = false;
                    int id = 0, failed = 0, lockSecs = 0;
                    string dbUser = null, hash = null, role = null;

                    using (var cmd = new MySqlCommand(
                        "SELECT user_id, username, password_hash, role, is_active, failed_attempts, " +
                        "(locked_until IS NOT NULL AND locked_until > NOW()) AS is_locked, " +
                        "IFNULL(TIMESTAMPDIFF(SECOND, NOW(), locked_until), 0) AS lock_secs " +
                        "FROM users WHERE username = @u LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@u", username);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                found = true;
                                id = Convert.ToInt32(r["user_id"]);
                                dbUser = Convert.ToString(r["username"]);
                                hash = Convert.ToString(r["password_hash"]);
                                role = Convert.ToString(r["role"]);
                                active = Convert.ToInt32(r["is_active"]) != 0;
                                failed = Convert.ToInt32(r["failed_attempts"]);
                                locked = Convert.ToInt32(r["is_locked"]) != 0;
                                lockSecs = Convert.ToInt32(r["lock_secs"]);
                            }
                        }
                    }

                    if (!found)
                    {
                        PasswordHasher.VerifyDummy(password);   // same cost as a real check
                        return new LoginResult { Status = LoginStatus.InvalidCredentials };
                    }

                    if (locked)
                        return new LoginResult { Status = LoginStatus.Locked, LockSecondsRemaining = Math.Max(lockSecs, 1) };

                    if (!PasswordHasher.Verify(password, hash))
                    {
                        int newCount = failed + 1;
                        bool lockNow = newCount >= MaxFailedAttempts;
                        using (var upd = new MySqlCommand(
                            "UPDATE users SET failed_attempts = @f, " +
                            "locked_until = CASE WHEN @l = 1 THEN DATE_ADD(NOW(), INTERVAL " + LockMinutes + " MINUTE) ELSE locked_until END " +
                            "WHERE user_id = @id", conn))
                        {
                            upd.Parameters.AddWithValue("@f", lockNow ? 0 : newCount);
                            upd.Parameters.AddWithValue("@l", lockNow ? 1 : 0);
                            upd.Parameters.AddWithValue("@id", id);
                            upd.ExecuteNonQuery();
                        }
                        return lockNow
                            ? new LoginResult { Status = LoginStatus.Locked, LockSecondsRemaining = LockMinutes * 60 }
                            : new LoginResult { Status = LoginStatus.InvalidCredentials };
                    }

                    if (!active)
                        return new LoginResult { Status = LoginStatus.Disabled };

                    // Success: reset the counters, stamp last_login, upgrade an old hash if needed.
                    string newHash = PasswordHasher.NeedsRehash(hash) ? PasswordHasher.Hash(password) : null;
                    using (var ok = new MySqlCommand(
                        "UPDATE users SET failed_attempts = 0, locked_until = NULL, last_login = NOW(), " +
                        "password_hash = IFNULL(@h, password_hash) WHERE user_id = @id", conn))
                    {
                        ok.Parameters.AddWithValue("@h", (object)newHash ?? DBNull.Value);
                        ok.Parameters.AddWithValue("@id", id);
                        ok.ExecuteNonQuery();
                    }

                    return new LoginResult { Status = LoginStatus.Success, Username = dbUser, Role = role };
                }
            }
            catch (MySqlException ex)
            {
                return new LoginResult { Status = LoginStatus.Error, Message = DbHelper.Describe(ex) };
            }
            catch (Exception)
            {
                return new LoginResult { Status = LoginStatus.Error, Message = "Something went wrong. Please try again." };
            }
        }

        public static RegisterResult Register(string fullName, string username, string email, string password, string role)
        {
            // Never trust the UI: re-check the role here.
            role = role == "Administrator" ? "Administrator" : "Staff";

            try
            {
                string hash = PasswordHasher.Hash(password);

                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(
                        "INSERT INTO users (full_name, username, email, password_hash, role) " +
                        "VALUES (@n, @u, @e, @p, @r)", conn))
                    {
                        cmd.Parameters.AddWithValue("@n", fullName.Trim());
                        cmd.Parameters.AddWithValue("@u", username.Trim());
                        cmd.Parameters.AddWithValue("@e", email.Trim().ToLowerInvariant());
                        cmd.Parameters.AddWithValue("@p", hash);
                        cmd.Parameters.AddWithValue("@r", role);
                        cmd.ExecuteNonQuery();
                    }
                }
                return new RegisterResult { Status = RegisterStatus.Success };
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)   // duplicate key: MySQL's UNIQUE constraint decides, no race condition
                {
                    string msg = ex.Message ?? "";
                    int k = msg.LastIndexOf("for key", StringComparison.OrdinalIgnoreCase);
                    string key = k >= 0 ? msg.Substring(k) : msg;
                    if (key.IndexOf("username", StringComparison.OrdinalIgnoreCase) >= 0)
                        return new RegisterResult { Status = RegisterStatus.UsernameTaken };
                    return new RegisterResult { Status = RegisterStatus.EmailTaken };
                }
                return new RegisterResult { Status = RegisterStatus.Error, Message = DbHelper.Describe(ex) };
            }
            catch (Exception)
            {
                return new RegisterResult { Status = RegisterStatus.Error, Message = "Something went wrong. Please try again." };
            }
        }
    }
}
