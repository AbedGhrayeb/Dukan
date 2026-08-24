namespace Dukan.Web.Domain.Entities;

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string EntityName { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? UserId { get; set; }

    public DateTime CreatedAt { get; set; }
}
