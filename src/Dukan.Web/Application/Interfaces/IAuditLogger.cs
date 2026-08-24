using Dukan.Web.Domain.Entities;

namespace Dukan.Web.Application.Interfaces;

public interface IAuditLogger
{
    Task LogAsync(
        string entityName,
        string entityId,
        string action,
        string? description,
        Guid? userId = null,
        CancellationToken ct = default);
}
