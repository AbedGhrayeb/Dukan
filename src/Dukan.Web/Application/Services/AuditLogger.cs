using Dukan.Web.Application.Interfaces;
using Dukan.Web.Data;
using Dukan.Web.Domain.Entities;

namespace Dukan.Web.Application.Services;

public sealed class AuditLogger(ApplicationDbContext db) : IAuditLogger
{
    public async Task LogAsync(
        string entityName,
        string entityId,
        string action,
        string? description,
        Guid? userId = null,
        CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            Description = description,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(ct);
    }
}
