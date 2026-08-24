namespace Dukan.Web.Domain.Entities;

/// <summary>
/// Per-subscription row storing Firebase service-account JSON for Remote Config.
/// Each Subscription has its own Firebase project (mobile app tenant).
/// Customer can have multiple subscriptions, each with unique Firebase config.
/// </summary>
public sealed class FirebaseConfig
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid SubscriptionId { get; set; }

    public Subscription Subscription { get; set; } = null!;

    /// <summary>
    /// Project id extracted from JSON (project_id field).
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Full service-account JSON content (as pasted). Stored as nvarchar(max).
    /// </summary>
    public string CredentialJson { get; set; } = string.Empty;

    public string? ClientEmail { get; set; }

    public bool Enabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? UpdatedBy { get; set; }
}
