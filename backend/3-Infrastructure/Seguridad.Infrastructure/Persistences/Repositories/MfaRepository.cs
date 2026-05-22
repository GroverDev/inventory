using System.Security.Cryptography;
using Common.Utilities.Exceptions;
using Dapper;
using Microsoft.Extensions.Options;
using Seguridad.Domain;

namespace Seguridad.Infrastructure;

public class MfaRepository(SeguridadDbContext _context, IOptions<MfaSettings> _options) : IMfaRepository
{
    private static readonly char[] RecoveryCodeChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

    public async Task<MfaInfo?> GetTotpMfa(int userId)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            const string sql = @"
                SELECT m.id, m.user_id, m.mfa_type, m.secret_encrypted, m.is_enabled, m.is_required,
                       m.failed_attempts, m.locked_until, u.email
                FROM sec.user_mfa m
                JOIN sec.users u ON u.id = m.user_id
                WHERE m.user_id = @user_id AND m.mfa_type = 'totp'";
            return await db.QueryFirstOrDefaultAsync<MfaInfo>(sql, new { user_id = userId });
        }
        finally { db.Close(); }
    }

    public async Task<MfaInfo?> GetTotpMfaByUuid(Guid userUuid)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            const string sql = @"
                SELECT m.id, m.user_id, m.mfa_type, m.secret_encrypted, m.is_enabled, m.is_required,
                       m.failed_attempts, m.locked_until, u.email
                FROM sec.user_mfa m
                JOIN sec.users u ON u.id = m.user_id
                WHERE u.uuid = @uuid AND m.mfa_type = 'totp' AND u.is_active";
            return await db.QueryFirstOrDefaultAsync<MfaInfo>(sql, new { uuid = userUuid });
        }
        finally { db.Close(); }
    }

    public async Task UpsertTotpSecret(int userId, string encryptedSecret)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            const string sql = @"
                INSERT INTO sec.user_mfa (user_id, mfa_type, secret_encrypted, is_enabled, is_required, updated_at)
                VALUES (@user_id, 'totp', @secret, false, false, now())
                ON CONFLICT (user_id, mfa_type)
                DO UPDATE SET secret_encrypted = @secret, is_enabled = false, updated_at = now()";
            await db.ExecuteAsync(sql, new { user_id = userId, secret = encryptedSecret });
        }
        finally { db.Close(); }
    }

    public async Task<List<string>> ActivateTotp(int userId)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            using var tx = db.BeginTransaction();
            try
            {
                const string getMfaId = @"
                    SELECT id FROM sec.user_mfa
                    WHERE user_id = @user_id AND mfa_type = 'totp'";
                int mfaId = await db.ExecuteScalarAsync<int>(getMfaId, new { user_id = userId }, tx);

                const string activate = @"
                    UPDATE sec.user_mfa
                    SET is_enabled = true, enabled_at = now(), failed_attempts = 0, locked_until = null, updated_at = now()
                    WHERE id = @mfa_id";
                await db.ExecuteAsync(activate, new { mfa_id = mfaId }, tx);

                // Rotate recovery codes
                await db.ExecuteAsync("DELETE FROM sec.user_mfa_recovery_codes WHERE user_mfa_id = @mfa_id",
                    new { mfa_id = mfaId }, tx);

                var settings = _options.Value;
                var plainCodes = new List<string>();

                for (int i = 0; i < 10; i++)
                {
                    var raw = GenerateRawCode();
                    var formatted = $"{raw[..5]}-{raw[5..]}";
                    plainCodes.Add(formatted);

                    var hash = Common.Utilities.Cryptography.Hash.HashPassword(raw);
                    await db.ExecuteAsync(
                        "INSERT INTO sec.user_mfa_recovery_codes (user_mfa_id, code_hash) VALUES (@mfa_id, @code_hash)",
                        new { mfa_id = mfaId, code_hash = hash }, tx);
                }

                tx.Commit();
                return plainCodes;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        finally { db.Close(); }
    }

    public async Task DisableTotp(int userId)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            using var tx = db.BeginTransaction();
            try
            {
                const string getMfaId = "SELECT id FROM sec.user_mfa WHERE user_id = @user_id AND mfa_type = 'totp'";
                int? mfaId = await db.ExecuteScalarAsync<int?>(getMfaId, new { user_id = userId }, tx);

                if (mfaId.HasValue)
                {
                    await db.ExecuteAsync("DELETE FROM sec.user_mfa_recovery_codes WHERE user_mfa_id = @mfa_id",
                        new { mfa_id = mfaId.Value }, tx);

                    await db.ExecuteAsync(@"
                        UPDATE sec.user_mfa
                        SET is_enabled = false, secret_encrypted = null, enabled_at = null,
                            failed_attempts = 0, locked_until = null, updated_at = now()
                        WHERE id = @mfa_id",
                        new { mfa_id = mfaId.Value }, tx);
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        finally { db.Close(); }
    }

    public async Task RecordFailure(int mfaId, int maxAttempts, int lockoutMinutes)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            const string sql = @"
                UPDATE sec.user_mfa
                SET failed_attempts = failed_attempts + 1,
                    locked_until = CASE
                        WHEN failed_attempts + 1 >= @max_attempts
                        THEN now() + (@lockout_minutes * INTERVAL '1 minute')
                        ELSE locked_until
                    END,
                    updated_at = now()
                WHERE id = @mfa_id";
            await db.ExecuteAsync(sql, new { mfa_id = mfaId, max_attempts = maxAttempts, lockout_minutes = lockoutMinutes });
        }
        finally { db.Close(); }
    }

    public async Task ResetAttempts(int mfaId)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            await db.ExecuteAsync(@"
                UPDATE sec.user_mfa
                SET failed_attempts = 0, locked_until = null, updated_at = now()
                WHERE id = @mfa_id",
                new { mfa_id = mfaId });
        }
        finally { db.Close(); }
    }

    public async Task<bool> UseRecoveryCode(int userId, string normalizedCode)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            const string getCodes = @"
                SELECT rc.id, rc.code_hash
                FROM sec.user_mfa_recovery_codes rc
                JOIN sec.user_mfa m ON m.id = rc.user_mfa_id
                WHERE m.user_id = @user_id AND m.mfa_type = 'totp'
                  AND rc.used_at IS NULL";

            var codes = (await db.QueryAsync<(int Id, string CodeHash)>(getCodes, new { user_id = userId })).ToList();

            foreach (var (id, hash) in codes)
            {
                if (!Common.Utilities.Cryptography.Hash.VerifyPassword(hash, normalizedCode))
                    continue;

                await db.ExecuteAsync(
                    "UPDATE sec.user_mfa_recovery_codes SET used_at = now() WHERE id = @id",
                    new { id });

                // Reset failed attempts after successful recovery
                await db.ExecuteAsync(@"
                    UPDATE sec.user_mfa
                    SET failed_attempts = 0, locked_until = null, updated_at = now()
                    WHERE user_id = @user_id AND mfa_type = 'totp'",
                    new { user_id = userId });

                return true;
            }

            return false;
        }
        finally { db.Close(); }
    }

    public async Task<int?> GetUserIdByUuid(Guid userUuid)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            return await db.ExecuteScalarAsync<int?>(
                "SELECT id FROM sec.users WHERE uuid = @uuid AND is_active",
                new { uuid = userUuid });
        }
        finally { db.Close(); }
    }

    public async Task AdminResetMfa(int userId)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            using var tx = db.BeginTransaction();
            try
            {
                const string getMfaId = "SELECT id FROM sec.user_mfa WHERE user_id = @user_id AND mfa_type = 'totp'";
                int? mfaId = await db.ExecuteScalarAsync<int?>(getMfaId, new { user_id = userId }, tx);

                if (mfaId.HasValue)
                {
                    await db.ExecuteAsync("DELETE FROM sec.user_mfa_recovery_codes WHERE user_mfa_id = @mfa_id",
                        new { mfa_id = mfaId.Value }, tx);
                    await db.ExecuteAsync("DELETE FROM sec.user_mfa WHERE id = @mfa_id",
                        new { mfa_id = mfaId.Value }, tx);
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        finally { db.Close(); }
    }

    public async Task AdminSetRequired(int userId, bool required)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            const string sql = @"
                INSERT INTO sec.user_mfa (user_id, mfa_type, is_enabled, is_required, updated_at)
                VALUES (@user_id, 'totp', false, @required, now())
                ON CONFLICT (user_id, mfa_type)
                DO UPDATE SET is_required = @required, updated_at = now()";
            await db.ExecuteAsync(sql, new { user_id = userId, required });
        }
        finally { db.Close(); }
    }

    private static string GenerateRawCode()
    {
        var bytes = new byte[10];
        RandomNumberGenerator.Fill(bytes);
        return new string(bytes.Select(b => RecoveryCodeChars[b % RecoveryCodeChars.Length]).ToArray());
    }
}
