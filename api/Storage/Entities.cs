using Azure;
using Azure.Data.Tables;

public sealed class UserEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "USER";
    public string RowKey { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string FullName { get; set; } = "";
    public string PasswordHash { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public ETag ETag { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}

public sealed class UserEmailIndexEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "EMAIL";
    public string RowKey { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public ETag ETag { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}

public sealed class SessionEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "SESSION";
    public string RowKey { get; set; } = default!;
    public string? UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public string? IpHash { get; set; }
    public string? UaHash { get; set; }
    public ETag ETag { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}
public sealed class EventBySessionEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public string? UserId { get; set; }
    public string EventType { get; set; } = default!;
    public string DataJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public ETag ETag { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}

public sealed class EventByUserEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public string SessionId { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public string DataJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public ETag ETag { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}
