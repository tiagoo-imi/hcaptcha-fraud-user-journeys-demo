using Azure.Data.Tables;
using System.Text.Json;

public sealed class TableStore
{
    private readonly TableServiceClient _svc;

    public TableStore(TableServiceClient svc)
    {
        _svc = svc;
    }

    // Small helper to keep table name usage consistent and centralized.
    private TableClient T(string name) => _svc.GetTableClient(name);

    public async Task EnsureAsync()
    {
        // Creates all tables required by the demo on startup.
        // This allows a fresh Azure Storage account to work without manual provisioning.
        await T("Users").CreateIfNotExistsAsync();
        await T("UsersByEmail").CreateIfNotExistsAsync();
        await T("Sessions").CreateIfNotExistsAsync();
        await T("EventsBySession").CreateIfNotExistsAsync();
        await T("EventsByUser").CreateIfNotExistsAsync();
    }

    public async Task<UserEntity?> GetUserByEmail(string email)
    {
        // Email lookup is done via a secondary index table to avoid table scans.
        // This mirrors a common pattern used with Azure Table Storage.
        var key = email.Trim().ToLowerInvariant();
        try
        {
            var idx = await T("UsersByEmail").GetEntityAsync<UserEmailIndexEntity>("EMAIL", key);
            var user = await T("Users").GetEntityAsync<UserEntity>("USER", idx.Value.UserId);
            return user.Value;
        }
        catch
        {
            // Any lookup failure is treated as "user not found" for demo simplicity.
            return null;
        }
    }

    public async Task<UserEntity?> GetUserById(string userId)
    {
        var key = userId.Trim();
        try
        {
            var user = await T("Users").GetEntityAsync<UserEntity>("USER", key);
            return user.Value;
        }
        catch
        {
            return null;
        }
    }

    public async Task<UserEntity> CreateUser(string email, string name, string hash)
    {
        // UserId generation is intentionally deterministic-ish for demo readability.
        // Not meant to be a production-grade identifier strategy.
        var id = Helpers.BuildUserId(name);
        var now = DateTimeOffset.UtcNow;

        var user = new UserEntity
        {
            RowKey = id,
            Email = email,
            FullName = name,
            PasswordHash = hash,
            CreatedAt = now
        };

        // Separate email index table enables fast lookups by email.
        //On a relational DB, this would not be necessary.
        var idx = new UserEmailIndexEntity
        {
            RowKey = email.Trim().ToLowerInvariant(),
            UserId = id,
            Email = email,
            CreatedAt = now
        };

        await T("Users").AddEntityAsync(user);
        await T("UsersByEmail").AddEntityAsync(idx);
        return user;
    }

    public async Task TouchSession(string sid)
    {
        var now = DateTimeOffset.UtcNow;
        try
        {
            // If the session already exists, just update activity timestamps.
            var s = await T("Sessions").GetEntityAsync<SessionEntity>("SESSION", sid);
            s.Value.LastSeenAt = now;

            // Clearing EndedAt allows the same sid to be reused after a refresh.
            s.Value.EndedAt = null;

            await T("Sessions").UpdateEntityAsync(s.Value, s.Value.ETag, TableUpdateMode.Merge);
        }
        catch
        {
            // First time we see this sid: create a new session record.
            await T("Sessions").AddEntityAsync(new SessionEntity
            {
                RowKey = sid,
                CreatedAt = now,
                LastSeenAt = now,
                EndedAt = null
            });
        }
    }

    public async Task BindSession(string sid, string userId)
    {
        // Explicitly binds a previously anonymous session to a user after login/signup.
        var s = await T("Sessions").GetEntityAsync<SessionEntity>("SESSION", sid);
        s.Value.UserId = userId;
        s.Value.LastSeenAt = DateTimeOffset.UtcNow;
        await T("Sessions").UpdateEntityAsync(s.Value, s.Value.ETag, TableUpdateMode.Merge);
    }

  
    public async Task EndSession(string sid)
    {
        if (string.IsNullOrWhiteSpace(sid)) return;

        var now = DateTimeOffset.UtcNow;
        var t = T("Sessions");

        var e = await t.GetEntityIfExistsAsync<SessionEntity>("SESSION", sid);
        if (!e.HasValue) return;

        // Marks the session as ended but keeps it queryable for post-analysis.
        var s = e.Value;
        s.LastSeenAt = now;
        s.EndedAt = now;

        await t.UpdateEntityAsync(s, s.ETag, TableUpdateMode.Merge);
    }

    public async Task UpdateUserPassword(string userId, string newHash)
    {
        // Explicit validation helps catch demo misuse early and keeps behavior predictable.
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("userId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(newHash))
            throw new ArgumentException("newHash is required.", nameof(newHash));

        var t = T("Users");

        var e = await t.GetEntityAsync<UserEntity>("USER", userId);
        e.Value.PasswordHash = newHash;

        await t.UpdateEntityAsync(e.Value, e.Value.ETag, TableUpdateMode.Merge);
    }
}
