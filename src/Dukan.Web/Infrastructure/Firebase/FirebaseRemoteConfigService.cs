using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dukan.Web.Application.Configuration;
using Dukan.Web.Application.DTOs.RemoteConfig;
using Dukan.Web.Application.Interfaces;
using Dukan.Web.Data;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dukan.Web.Infrastructure.Firebase;

public sealed class FirebaseRemoteConfigService : IFirebaseRemoteConfigService
{
    private const string RemoteConfigScope = "https://www.googleapis.com/auth/firebase.remoteconfig";
    private const string CloudPlatformScope = "https://www.googleapis.com/auth/cloud-platform";

    private readonly FirebaseSettings _settings;
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<FirebaseRemoteConfigService> _logger;
    private readonly IHttpClientFactory _httpFactory;
    private readonly RemoteConfigDraftStore _draftStore;

    // Legacy global check — now per-subscription, keep for compat (checks if ANY subscription has config)
    public bool IsConfigured => string.IsNullOrEmpty(ConfigurationError);
    public string? ConfigurationError
    {
        get
        {
            if (!_settings.Enabled) return "Firebase معطل (Firebase:Enabled=false).";
            try
            {
                var any = _db.FirebaseConfigs.AsNoTracking().Any(x => !string.IsNullOrWhiteSpace(x.CredentialJson));
                if (any) return null;
            }
            catch { }
            if (!string.IsNullOrWhiteSpace(_settings.ProjectId) ||
                !string.IsNullOrWhiteSpace(_settings.ServiceAccountPath) ||
                !string.IsNullOrWhiteSpace(_settings.CredentialJson))
                return null;
            return "Firebase غير مُهيأ. الصق ملف Service Account JSON في صفحة تهيئة Firebase لكل اشتراك.";
        }
    }

    private string? _resolvedProjectId;

    public FirebaseRemoteConfigService(
        IOptions<FirebaseSettings> options,
        ApplicationDbContext db,
        IAuditLogger auditLogger,
        ILogger<FirebaseRemoteConfigService> logger,
        IHttpClientFactory httpFactory,
        RemoteConfigDraftStore draftStore)
    {
        _settings = options.Value;
        _db = db;
        _auditLogger = auditLogger;
        _logger = logger;
        _httpFactory = httpFactory;
        _draftStore = draftStore;
        _resolvedProjectId = ResolveProjectIdFromSettings();
    }

    private string? ResolveProjectIdFromSettings()
    {
        if (!string.IsNullOrWhiteSpace(_settings.ProjectId)) return _settings.ProjectId.Trim();
        if (!string.IsNullOrWhiteSpace(_settings.CredentialJson))
        {
            try { using var doc = JsonDocument.Parse(_settings.CredentialJson); if (doc.RootElement.TryGetProperty("project_id", out var pid)) return pid.GetString(); } catch { }
        }
        if (!string.IsNullOrWhiteSpace(_settings.ServiceAccountPath) && File.Exists(_settings.ServiceAccountPath))
        {
            try { var json = File.ReadAllText(_settings.ServiceAccountPath); using var doc = JsonDocument.Parse(json); if (doc.RootElement.TryGetProperty("project_id", out var pid)) return pid.GetString(); } catch { }
        }
        return null;
    }

    public async Task<bool> IsConfiguredAsync(Guid subscriptionId, CancellationToken ct = default)
        => string.IsNullOrEmpty(await GetConfigurationErrorAsync(subscriptionId, ct).ConfigureAwait(false));

    public async Task<string?> GetConfigurationErrorAsync(Guid subscriptionId, CancellationToken ct = default)
    {
        if (subscriptionId == Guid.Empty) return "الاشتراك غير محدد.";
        if (!_settings.Enabled) return "Firebase معطل (Firebase:Enabled=false).";
        var cfg = await _db.FirebaseConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.SubscriptionId == subscriptionId, ct);
        if (cfg != null && !string.IsNullOrWhiteSpace(cfg.CredentialJson)) return null;
        var sub = await _db.Subscriptions.AsNoTracking().Include(s => s.Customer).Include(s => s.Plan).FirstOrDefaultAsync(s => s.Id == subscriptionId, ct);
        var name = sub != null ? $"{sub.Customer.FullName} - {sub.Plan.Name}" : subscriptionId.ToString();
        return $"Firebase غير مُهيأ للاشتراك {name}. الصق ملف JSON في صفحة تهيئة Firebase.";
    }

    private async Task<string> GetProjectIdAsync(Guid subscriptionId, CancellationToken ct)
    {
        var cfg = await _db.FirebaseConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.SubscriptionId == subscriptionId, ct);
        if (cfg != null && !string.IsNullOrWhiteSpace(cfg.CredentialJson))
        {
            try { using var doc = JsonDocument.Parse(cfg.CredentialJson); if (doc.RootElement.TryGetProperty("project_id", out var pid) && !string.IsNullOrWhiteSpace(pid.GetString())) return pid.GetString()!; } catch { }
            if (!string.IsNullOrWhiteSpace(cfg.ProjectId)) return cfg.ProjectId;
        }
        throw new InvalidOperationException("Firebase غير مُهيأ لهذا الاشتراك. الصق JSON في صفحة تهيئة Firebase للاشتراك.");
    }

    private async Task<GoogleCredential> GetCredentialAsync(Guid subscriptionId, CancellationToken ct)
    {
        var cfg = await _db.FirebaseConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.SubscriptionId == subscriptionId, ct);
        if (cfg != null && !string.IsNullOrWhiteSpace(cfg.CredentialJson))
        {
            var cred = GoogleCredential.FromJson(cfg.CredentialJson);
            if (cred.IsCreateScopedRequired) cred = cred.CreateScoped([RemoteConfigScope, CloudPlatformScope]);
            return cred;
        }
        throw new InvalidOperationException("Firebase غير مُهيأ لهذا الاشتراك. الصق JSON في صفحة تهيئة Firebase للاشتراك.");
    }

    private async Task<string> GetAccessTokenAsync(Guid subscriptionId, CancellationToken ct)
    {
        var cred = await GetCredentialAsync(subscriptionId, ct).ConfigureAwait(false);
        var token = await cred.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("فشل الحصول على Access Token من Google Credential.");
        return token;
    }

    private async Task EnsureConfiguredAsync(Guid subscriptionId, CancellationToken ct = default)
    {
        var err = await GetConfigurationErrorAsync(subscriptionId, ct).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(err)) throw new InvalidOperationException(err);
        _ = await GetProjectIdAsync(subscriptionId, ct).ConfigureAwait(false);
    }

    private sealed record RemoteConfigFetchResult(Dictionary<string, (string Value, string? Description)> Parameters, string ETag, long VersionNumber);

    private async Task<RemoteConfigFetchResult> FetchRemoteAsync(Guid subscriptionId, CancellationToken ct)
    {
        await EnsureConfiguredAsync(subscriptionId, ct).ConfigureAwait(false);
        var projectId = await GetProjectIdAsync(subscriptionId, ct).ConfigureAwait(false);
        var token = await GetAccessTokenAsync(subscriptionId, ct).ConfigureAwait(false);
        var client = _httpFactory.CreateClient("FirebaseRemoteConfig");
        var url = $"https://firebaseremoteconfig.googleapis.com/v1/projects/{projectId}/remoteConfig";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var resp = await client.SendAsync(req, ct).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException($"Firebase Remote Config GET failed {(int)resp.StatusCode} {resp.ReasonPhrase}: {body}");
        var etag = resp.Headers.ETag?.Tag?.Trim('"') ?? "";
        if (string.IsNullOrEmpty(etag) && resp.Headers.TryGetValues("ETag", out var v1)) etag = v1.FirstOrDefault()?.Trim('"') ?? "";
        if (string.IsNullOrEmpty(etag) && resp.Headers.TryGetValues("etag", out var v2)) etag = v2.FirstOrDefault()?.Trim('"') ?? "";
        if (string.IsNullOrEmpty(etag))
        {
            try { using var doc2 = JsonDocument.Parse(body); if (doc2.RootElement.TryGetProperty("etag", out var et)) etag = et.GetString()?.Trim('"') ?? ""; } catch { }
        }
        if (string.IsNullOrEmpty(etag)) _logger.LogWarning("Fetched Remote Config without ETag for subscription {SubscriptionId}", subscriptionId);
        else _logger.LogDebug("Fetched Remote Config ETag={ETag} for subscription {SubscriptionId}", etag, subscriptionId);
        var parameters = new Dictionary<string, (string Value, string? Description)>();
        long versionNumber = 0;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("parameters", out var paramsEl) && paramsEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in paramsEl.EnumerateObject())
                {
                    string val = ""; string? desc = null;
                    if (prop.Value.TryGetProperty("defaultValue", out var dv) && dv.ValueKind == JsonValueKind.Object)
                    {
                        if (dv.TryGetProperty("value", out var vEl)) val = vEl.GetString() ?? "";
                        else if (dv.TryGetProperty("useInAppDefault", out var use) && use.GetBoolean()) val = "";
                    }
                    if (prop.Value.TryGetProperty("description", out var dEl)) desc = dEl.GetString();
                    parameters[prop.Name] = (val, desc);
                }
            }
            if (doc.RootElement.TryGetProperty("version", out var verEl) && verEl.TryGetProperty("versionNumber", out var vn))
            {
                var s = vn.GetString(); long.TryParse(s, out versionNumber);
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to parse Remote Config JSON for subscription {SubscriptionId}. Body: {Body}", subscriptionId, body.Length > 1000 ? body[..1000] : body); }
        return new RemoteConfigFetchResult(parameters, etag, versionNumber);
    }

    private async Task PublishRemoteAsync(Guid subscriptionId, Dictionary<string, (string Value, string? Description)> mergedParameters, string etag, CancellationToken ct)
    {
        var projectId = await GetProjectIdAsync(subscriptionId, ct).ConfigureAwait(false);
        var token = await GetAccessTokenAsync(subscriptionId, ct).ConfigureAwait(false);
        var client = _httpFactory.CreateClient("FirebaseRemoteConfig");
        var url = $"https://firebaseremoteconfig.googleapis.com/v1/projects/{projectId}/remoteConfig";
        var payload = new Dictionary<string, object>
        {
            ["parameters"] = mergedParameters.ToDictionary(kv => kv.Key, kv => (object)new Dictionary<string, object> { ["defaultValue"] = new Dictionary<string, string> { ["value"] = kv.Value.Value }, ["description"] = kv.Value.Description ?? "" }),
            ["conditions"] = Array.Empty<object>(),
            ["parameterGroups"] = new Dictionary<string, object>()
        };
        var json = JsonSerializer.Serialize(payload);
        using var req = new HttpRequestMessage(HttpMethod.Put, url) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var ifMatch = string.IsNullOrWhiteSpace(etag) ? "*" : etag.Trim('"');
        req.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        _logger.LogInformation("Publishing Remote Config for subscription {SubscriptionId} project {ProjectId} with If-Match={ETag} ({Count} params)", subscriptionId, projectId, ifMatch, mergedParameters.Count);
        using var resp = await client.SendAsync(req, ct).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var msg = $"Publish failed {(int)resp.StatusCode} {resp.ReasonPhrase}: {body}";
            if (resp.StatusCode == System.Net.HttpStatusCode.PreconditionFailed || body.Contains("etag", StringComparison.OrdinalIgnoreCase) || body.Contains("ABORTED"))
                throw new InvalidOperationException("Version mismatch (ETag). " + body);
            throw new HttpRequestException(msg);
        }
    }

    public async Task<RemoteConfigTemplateDto> GetTemplateAsync(Guid subscriptionId, CancellationToken ct = default)
    {
        await EnsureConfiguredAsync(subscriptionId, ct).ConfigureAwait(false);
        var fetched = await FetchRemoteAsync(subscriptionId, ct).ConfigureAwait(false);
        var parameters = fetched.Parameters.Select(kv => new RemoteConfigParameterDto(kv.Key, kv.Value.Value, InferType(kv.Value.Value), kv.Value.Description, null)).OrderBy(p => p.Key).ToList();
        var draft = _draftStore.Get(subscriptionId);
        await draft.Lock.WaitAsync(ct);
        try
        {
            if (draft.Parameters != null || draft.DeletedKeys.Count > 0)
            {
                var merged = new Dictionary<string, RemoteConfigParameterDto>();
                foreach (var p in parameters) if (!draft.DeletedKeys.Contains(p.Key)) merged[p.Key] = p;
                if (draft.Parameters != null) foreach (var kv in draft.Parameters) merged[kv.Key] = kv.Value;
                var hasChanges = merged.Count != fetched.Parameters.Count || draft.DeletedKeys.Count > 0 || (draft.Parameters != null && draft.Parameters.Any(kv => !fetched.Parameters.TryGetValue(kv.Key, out var orig) || orig.Value != kv.Value.Value));
                return new RemoteConfigTemplateDto(merged.Values.OrderBy(x => x.Key).ToList(), fetched.ETag, fetched.VersionNumber, null, hasChanges);
            }
        }
        finally { draft.Lock.Release(); }
        return new RemoteConfigTemplateDto(parameters, fetched.ETag, fetched.VersionNumber, null, false);
    }

    public async Task<IReadOnlyList<RemoteConfigParameterDto>> ListParametersAsync(Guid subscriptionId, CancellationToken ct = default) => (await GetTemplateAsync(subscriptionId, ct).ConfigureAwait(false)).Parameters;
    public async Task<RemoteConfigParameterDto?> GetParameterAsync(Guid subscriptionId, string key, CancellationToken ct = default) => (await ListParametersAsync(subscriptionId, ct).ConfigureAwait(false)).FirstOrDefault(p => p.Key == key);

    public async Task<(bool Ok, string? Error)> UpsertDraftAsync(Guid subscriptionId, RemoteConfigUpsertForm form, Guid? userId, CancellationToken ct = default)
    {
        await EnsureConfiguredAsync(subscriptionId, ct).ConfigureAwait(false);
        var validation = ValidateForm(form); if (validation != null) return (false, validation);
        RemoteConfigFetchResult fetched; try { fetched = await FetchRemoteAsync(subscriptionId, ct).ConfigureAwait(false); } catch (Exception ex) { _logger.LogError(ex, "Failed to fetch template for draft upsert subscription {SubscriptionId}", subscriptionId); return (false, "تعذر جلب القالب من Firebase: " + MapFirebaseError(ex)); }
        var draft = _draftStore.Get(subscriptionId);
        await draft.Lock.WaitAsync(ct);
        try
        {
            draft.Parameters ??= fetched.Parameters.ToDictionary(kv => kv.Key, kv => new RemoteConfigParameterDto(kv.Key, kv.Value.Value, InferType(kv.Value.Value), kv.Value.Description, null));
            draft.DeletedKeys.Remove(form.Key);
            draft.ETag = fetched.ETag;
            draft.Parameters[form.Key] = new RemoteConfigParameterDto(form.Key, form.Value.Trim(), form.ValueType, form.Description?.Trim(), DateTime.UtcNow);
            _logger.LogInformation("Draft upsert: {Key} for subscription {SubscriptionId} by {UserId}", form.Key, subscriptionId, userId);
        }
        finally { draft.Lock.Release(); }
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteDraftAsync(Guid subscriptionId, string key, Guid? userId, CancellationToken ct = default)
    {
        await EnsureConfiguredAsync(subscriptionId, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(key)) return (false, "المفتاح مطلوب.");
        RemoteConfigFetchResult fetched; try { fetched = await FetchRemoteAsync(subscriptionId, ct).ConfigureAwait(false); } catch (Exception ex) { return (false, "تعذر جلب القالب: " + MapFirebaseError(ex)); }
        var draft = _draftStore.Get(subscriptionId);
        await draft.Lock.WaitAsync(ct);
        try
        {
            draft.Parameters ??= fetched.Parameters.ToDictionary(kv => kv.Key, kv => new RemoteConfigParameterDto(kv.Key, kv.Value.Value, InferType(kv.Value.Value), kv.Value.Description, null));
            draft.ETag = fetched.ETag;
            var existsInDraft = draft.Parameters.ContainsKey(key);
            var existsInPublished = fetched.Parameters.ContainsKey(key);
            if (!existsInDraft && !existsInPublished) return (false, "المفتاح غير موجود.");
            draft.Parameters.Remove(key);
            if (existsInPublished) draft.DeletedKeys.Add(key);
            _logger.LogInformation("Draft delete: {Key} for subscription {SubscriptionId} by {UserId}", key, subscriptionId, userId);
        }
        finally { draft.Lock.Release(); }
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> PublishAsync(Guid subscriptionId, Guid? userId, CancellationToken ct = default)
    {
        await EnsureConfiguredAsync(subscriptionId, ct).ConfigureAwait(false);
        var draft = _draftStore.Get(subscriptionId);
        await draft.Lock.WaitAsync(ct);
        Dictionary<string, RemoteConfigParameterDto>? draftCopy; HashSet<string> deletedCopy; string? draftEtag;
        try
        {
            if (draft.Parameters == null && draft.DeletedKeys.Count == 0) return (false, "لا توجد تغييرات لنشرها.");
            draftCopy = draft.Parameters != null ? new Dictionary<string, RemoteConfigParameterDto>(draft.Parameters) : null;
            deletedCopy = new HashSet<string>(draft.DeletedKeys);
            draftEtag = draft.ETag;
        }
        finally { draft.Lock.Release(); }
        RemoteConfigFetchResult fetched; try { fetched = await FetchRemoteAsync(subscriptionId, ct).ConfigureAwait(false); } catch (Exception ex) { return (false, "تعذر جلب القالب: " + MapFirebaseError(ex)); }
        if (!string.IsNullOrEmpty(draftEtag) && !string.IsNullOrEmpty(fetched.ETag) && draftEtag != fetched.ETag) return (false, "القالب تغير على Firebase (ETag mismatch). حدّث الصفحة وحاول مرة أخرى.");
        var merged = new Dictionary<string, (string Value, string? Description)>();
        foreach (var kv in fetched.Parameters) if (!deletedCopy.Contains(kv.Key)) merged[kv.Key] = kv.Value;
        if (draftCopy != null) foreach (var kv in draftCopy) merged[kv.Key] = (kv.Value.Value, kv.Value.Description);
        try
        {
            await PublishRemoteAsync(subscriptionId, merged, fetched.ETag, ct).ConfigureAwait(false);
            await draft.Lock.WaitAsync(ct);
            try { draft.Clear(); }
            finally { draft.Lock.Release(); }
            await _auditLogger.LogAsync("RemoteConfig", $"subscription:{subscriptionId}", "RemoteConfig.Published", $"تم نشر قالب Remote Config للاشتراك {subscriptionId} ({merged.Count} مفتاح).", userId, ct);
            _logger.LogInformation("Remote Config published for subscription {SubscriptionId} by {UserId}: {Count} params", subscriptionId, userId, merged.Count);
            return (true, null);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Version mismatch")) { _logger.LogWarning(ex, "Publish ETag mismatch for subscription {SubscriptionId}", subscriptionId); return (false, "فشل النشر: القالب تغير (Version mismatch). حدّث الصفحة وحاول مجدداً."); }
        catch (Exception ex) { _logger.LogError(ex, "Publish failed for subscription {SubscriptionId}", subscriptionId); return (false, "فشل النشر: " + MapFirebaseError(ex)); }
    }

    public async Task<(bool Ok, string? Error)> DiscardDraftAsync(Guid subscriptionId, CancellationToken ct = default)
    {
        var draft = _draftStore.Get(subscriptionId);
        await draft.Lock.WaitAsync(ct);
        try { draft.Clear(); }
        finally { draft.Lock.Release(); }
        return (true, null);
    }

    public Task<IReadOnlyList<RemoteConfigVersionDto>> ListVersionsAsync(Guid subscriptionId, int limit = 20, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<RemoteConfigVersionDto>>([]);

    public async Task<(bool Ok, string? Error)> SetIsActiveAsync(Guid subscriptionId, bool isActive, Guid? userId, CancellationToken ct = default)
    {
        await EnsureConfiguredAsync(subscriptionId, ct).ConfigureAwait(false);
        RemoteConfigFetchResult fetched;
        try { fetched = await FetchRemoteAsync(subscriptionId, ct).ConfigureAwait(false); }
        catch (Exception ex) { return (false, "تعذر جلب القالب: " + MapFirebaseError(ex)); }

        var val = isActive ? "true" : "false";
        // If already correct value, no need to publish
        if (fetched.Parameters.TryGetValue("is_active", out var cur) && cur.Value == val)
            return (true, null);

        var draft = _draftStore.Get(subscriptionId);
        await draft.Lock.WaitAsync(ct);
        try
        {
            draft.Parameters ??= fetched.Parameters.ToDictionary(kv => kv.Key, kv => new RemoteConfigParameterDto(kv.Key, kv.Value.Value, InferType(kv.Value.Value), kv.Value.Description, null));
            draft.ETag = fetched.ETag;
            draft.Parameters["is_active"] = new RemoteConfigParameterDto("is_active", val, "boolean", "حالة تفعيل الاشتراك", DateTime.UtcNow);
        }
        finally { draft.Lock.Release(); }

        var (ok, err) = await PublishAsync(subscriptionId, userId, ct);
        if (!ok) return (false, err);
        _logger.LogInformation("Set is_active={IsActive} for subscription {SubscriptionId}", val, subscriptionId);
        return (true, null);
    }

    private static string? ValidateForm(RemoteConfigUpsertForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Key)) return "المفتاح مطلوب.";
        if (!System.Text.RegularExpressions.Regex.IsMatch(form.Key, @"^[a-zA-Z][a-zA-Z0-9_]*$")) return "المفتاح يجب أن يبدأ بحرف ويحتوي فقط على حروف وأرقام و _.";
        if (form.Value == null) return "القيمة مطلوبة.";
        var vt = form.ValueType?.ToLowerInvariant();
        if (vt == "boolean" && form.Value.Trim().ToLowerInvariant() is not "true" and not "false") return "قيمة boolean يجب أن تكون true أو false.";
        if (vt == "number" && !double.TryParse(form.Value.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _)) return "قيمة number غير صالحة.";
        if (vt == "json") { try { JsonDocument.Parse(form.Value); } catch { return "قيمة JSON غير صالحة."; } }
        return null;
    }

    private static string InferType(string value)
    {
        var v = value.Trim();
        if (v.Equals("true", StringComparison.OrdinalIgnoreCase) || v.Equals("false", StringComparison.OrdinalIgnoreCase)) return "boolean";
        if (double.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _)) return "number";
        if ((v.StartsWith("{") && v.EndsWith("}")) || (v.StartsWith("[") && v.EndsWith("]"))) { try { JsonDocument.Parse(v); return "json"; } catch { } }
        return "string";
    }

    private static string MapFirebaseError(Exception ex)
    {
        var msg = ex.Message;
        if (msg.Contains("PERMISSION_DENIED") || msg.Contains("403")) return "صلاحيات مرفوضة — تأكد أن Service Account لديه دور Firebase Remote Config Admin.";
        if (msg.Contains("UNAUTHENTICATED") || msg.Contains("401")) return "مصادقة فاشلة — تحقق من ملف Service Account JSON.";
        if (msg.Contains("NOT_FOUND") || msg.Contains("404")) return "المشروع غير موجود — تحقق من Firebase:ProjectId.";
        if (msg.Contains("RESOURCE_EXHAUSTED") || msg.Contains("429")) return "تم تجاوز حد النشر — حاول لاحقاً (حد Firebase ~10 عمليات نشر/ساعة).";
        return msg.Length > 400 ? msg[..400] : msg;
    }
}
