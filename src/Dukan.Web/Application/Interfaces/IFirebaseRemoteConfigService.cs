using Dukan.Web.Application.DTOs.RemoteConfig;

namespace Dukan.Web.Application.Interfaces;

public interface IFirebaseRemoteConfigService
{
    Task<bool> IsConfiguredAsync(Guid subscriptionId, CancellationToken ct = default);

    Task<string?> GetConfigurationErrorAsync(Guid subscriptionId, CancellationToken ct = default);

    // Keep sync for backward compat in non-customer contexts (returns false)
    bool IsConfigured { get; }
    string? ConfigurationError { get; }

    Task<RemoteConfigTemplateDto> GetTemplateAsync(Guid subscriptionId, CancellationToken ct = default);

    Task<IReadOnlyList<RemoteConfigParameterDto>> ListParametersAsync(Guid subscriptionId, CancellationToken ct = default);

    Task<RemoteConfigParameterDto?> GetParameterAsync(Guid subscriptionId, string key, CancellationToken ct = default);

    /// <summary>Draft-only: modifies local template copy, does NOT publish.</summary>
    Task<(bool Ok, string? Error)> UpsertDraftAsync(Guid subscriptionId, RemoteConfigUpsertForm form, Guid? userId, CancellationToken ct = default);

    Task<(bool Ok, string? Error)> DeleteDraftAsync(Guid subscriptionId, string key, Guid? userId, CancellationToken ct = default);

    Task<(bool Ok, string? Error)> PublishAsync(Guid subscriptionId, Guid? userId, CancellationToken ct = default);

    Task<(bool Ok, string? Error)> DiscardDraftAsync(Guid subscriptionId, CancellationToken ct = default);

    Task<IReadOnlyList<RemoteConfigVersionDto>> ListVersionsAsync(Guid subscriptionId, int limit = 20, CancellationToken ct = default);

    /// <summary>Sets is_active to true/false directly on Firebase (used for activation/expiry).</summary>
    Task<(bool Ok, string? Error)> SetIsActiveAsync(Guid subscriptionId, bool isActive, Guid? userId, CancellationToken ct = default);
}
