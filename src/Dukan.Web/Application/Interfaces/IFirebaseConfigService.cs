using Dukan.Web.Domain.Entities;

namespace Dukan.Web.Application.Interfaces;

public interface IFirebaseConfigService
{
    Task<FirebaseConfig?> GetAsync(Guid subscriptionId, CancellationToken ct = default);

    /// <summary>Returns sanitized view model (masked JSON preview).</summary>
    Task<FirebaseConfigDto?> GetDtoAsync(Guid subscriptionId, CancellationToken ct = default);

    Task<IReadOnlyList<FirebaseConfigListItemDto>> ListAsync(string? search, int page, int pageSize, CancellationToken ct = default);

    Task<(bool Ok, string? Error)> SaveAsync(Guid subscriptionId, string credentialJson, Guid? userId, CancellationToken ct = default);

    Task<(bool Ok, string? Error)> DeleteAsync(Guid subscriptionId, Guid? userId, CancellationToken ct = default);

    Task<(bool Ok, string? Message)> TestConnectionAsync(Guid subscriptionId, CancellationToken ct = default);

    /// <summary>Test a raw JSON without saving (for validation before save).</summary>
    Task<(bool Ok, string? Message)> TestJsonAsync(string credentialJson, CancellationToken ct = default);
}

public sealed record FirebaseConfigDto(
    Guid Id,
    Guid SubscriptionId,
    Guid CustomerId,
    string CustomerName,
    string StoreName,
    string PlanName,
    string ProjectId,
    string? ClientEmail,
    bool Enabled,
    DateTime UpdatedAt,
    Guid? UpdatedBy,
    string MaskedJsonPreview,
    bool HasCredential);

public sealed record FirebaseConfigListItemDto(
    Guid SubscriptionId,
    Guid CustomerId,
    string CustomerName,
    string StoreName,
    string Phone,
    string PlanName,
    string Status,
    string? ProjectId,
    string? ClientEmail,
    bool HasConfig,
    DateTime? UpdatedAt);
