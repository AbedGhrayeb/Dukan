using System.Text.Json;
using Dukan.Web.Application.Interfaces;
using Dukan.Web.Data;
using Dukan.Web.Domain.Entities;
using Dukan.Web.Infrastructure.Firebase;
using Microsoft.EntityFrameworkCore;
namespace Dukan.Web.Application.Services;

public sealed class FirebaseConfigService(
    ApplicationDbContext db,
    IAuditLogger auditLogger,
    IHttpClientFactory httpFactory,
    RemoteConfigDraftStore draftStore,
    ILogger<FirebaseConfigService> logger) : IFirebaseConfigService
{
    public async Task<FirebaseConfig?> GetAsync(Guid subscriptionId, CancellationToken ct = default)
        => await db.FirebaseConfigs.AsNoTracking()
            .Include(x => x.Subscription).ThenInclude(s => s.Customer)
            .Include(x => x.Subscription).ThenInclude(s => s.Plan)
            .FirstOrDefaultAsync(x => x.SubscriptionId == subscriptionId, ct);

    public async Task<FirebaseConfigDto?> GetDtoAsync(Guid subscriptionId, CancellationToken ct = default)
    {
        var entity = await db.FirebaseConfigs.AsNoTracking()
            .Include(x => x.Subscription).ThenInclude(s => s.Customer)
            .Include(x => x.Subscription).ThenInclude(s => s.Plan)
            .FirstOrDefaultAsync(x => x.SubscriptionId == subscriptionId, ct);
        if (entity is null) return null;
        return new FirebaseConfigDto(
            entity.Id,
            entity.SubscriptionId,
            entity.Subscription.CustomerId,
            entity.Subscription.Customer?.FullName ?? entity.SubscriptionId.ToString(),
            entity.Subscription.Customer?.StoreName ?? string.Empty,
            entity.Subscription.Plan?.Name ?? string.Empty,
            entity.ProjectId,
            entity.ClientEmail,
            entity.Enabled,
            entity.UpdatedAt,
            entity.UpdatedBy,
            MaskJson(entity.CredentialJson),
            !string.IsNullOrWhiteSpace(entity.CredentialJson));
    }

    public async Task<IReadOnlyList<FirebaseConfigListItemDto>> ListAsync(string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Subscriptions.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => s.Customer.FullName.Contains(term) || s.Customer.StoreName.Contains(term) || s.Customer.Phone.Contains(term) || s.Plan.Name.Contains(term));
        }

        var subs = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new { s.Id, s.CustomerId, CustomerName = s.Customer.FullName, StoreName = s.Customer.StoreName, Phone = s.Customer.Phone, PlanName = s.Plan.Name, Status = s.Status.ToString() })
            .ToListAsync(ct);

        var subIds = subs.Select(s => s.Id).ToList();
        var configs = await db.FirebaseConfigs.AsNoTracking()
            .Where(f => subIds.Contains(f.SubscriptionId))
            .ToDictionaryAsync(f => f.SubscriptionId, ct);

        return subs.Select(s =>
        {
            configs.TryGetValue(s.Id, out var cfg);
            return new FirebaseConfigListItemDto(
                s.Id,
                s.CustomerId,
                s.CustomerName,
                s.StoreName,
                s.Phone,
                s.PlanName,
                s.Status,
                cfg?.ProjectId,
                cfg?.ClientEmail,
                cfg != null && !string.IsNullOrWhiteSpace(cfg.CredentialJson),
                cfg?.UpdatedAt);
        }).ToList();
    }

    public async Task<(bool Ok, string? Error)> SaveAsync(Guid subscriptionId, string credentialJson, Guid? userId, CancellationToken ct = default)
    {
        if (subscriptionId == Guid.Empty)
            return (false, "الاشتراك غير محدد.");

        if (string.IsNullOrWhiteSpace(credentialJson))
            return (false, "محتوى JSON مطلوب.");

        credentialJson = credentialJson.Trim();

        var subscription = await db.Subscriptions.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, ct);
        if (subscription is null)
            return (false, "الاشتراك غير موجود.");

        var validation = ValidateCredentialJson(credentialJson, out var projectId, out var clientEmail);
        if (validation != null) return (false, validation);

        // Unique by projectId: prevent same Firebase project assigned to multiple subscriptions
        var duplicate = await db.FirebaseConfigs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProjectId == projectId && x.SubscriptionId != subscriptionId, ct);
        if (duplicate != null)
        {
            var dupSub = await db.Subscriptions.AsNoTracking().Include(s => s.Customer).FirstOrDefaultAsync(s => s.Id == duplicate.SubscriptionId, ct);
            var dupName = dupSub != null ? $"{dupSub.Customer.FullName} ({dupSub.Plan.Name})" : duplicate.SubscriptionId.ToString();
            return (false, $"معرّف المشروع '{projectId}' مُستخدم بالفعل للاشتراك '{dupName}'. لا يمكن ربط نفس مشروع Firebase بأكثر من اشتراك. استخدم مشروع Firebase منفصل لكل اشتراك.");
        }

        var existing = await db.FirebaseConfigs.FirstOrDefaultAsync(x => x.SubscriptionId == subscriptionId, ct);
        if (existing is null)
        {
            existing = new FirebaseConfig
            {
                SubscriptionId = subscriptionId,
                ProjectId = projectId!,
                ClientEmail = clientEmail,
                CredentialJson = credentialJson,
                Enabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userId
            };
            db.FirebaseConfigs.Add(existing);
        }
        else
        {
            existing.ProjectId = projectId!;
            existing.ClientEmail = clientEmail;
            existing.CredentialJson = credentialJson;
            existing.Enabled = true;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = userId;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_FirebaseConfigs_ProjectId", StringComparison.OrdinalIgnoreCase) == true || ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true)
        {
            return (false, $"معرّف المشروع '{projectId}' مُستخدم بالفعل لاشتراك آخر. لا يمكن تكرار نفس projectId.");
        }
        // Clear any draft for this subscription because project may have changed
        draftStore.Get(subscriptionId).Clear();
        await auditLogger.LogAsync("FirebaseConfig", existing.Id.ToString(), "FirebaseConfig.Saved",
            $"تم حفظ إعدادات Firebase للاشتراك {subscription.Customer.FullName} ({subscription.Plan.Name}) (project: {existing.ProjectId}, email: {existing.ClientEmail})", userId, ct);
        logger.LogInformation("Firebase config saved for subscription {SubscriptionId} project {ProjectId} by {UserId}", subscriptionId, existing.ProjectId, userId);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteAsync(Guid subscriptionId, Guid? userId, CancellationToken ct = default)
    {
        var existing = await db.FirebaseConfigs.FirstOrDefaultAsync(x => x.SubscriptionId == subscriptionId, ct);
        if (existing is null) return (false, "لا توجد إعدادات لهذا الاشتراك لحذفها.");

        db.FirebaseConfigs.Remove(existing);
        await db.SaveChangesAsync(ct);
        draftStore.Remove(subscriptionId);
        await auditLogger.LogAsync("FirebaseConfig", existing.Id.ToString(), "FirebaseConfig.Deleted",
            $"تم حذف إعدادات Firebase للاشتراك {existing.SubscriptionId}", userId, ct);
        logger.LogInformation("Firebase config deleted for subscription {SubscriptionId} by {UserId}", subscriptionId, userId);
        return (true, null);
    }

    public async Task<(bool Ok, string? Message)> TestConnectionAsync(Guid subscriptionId, CancellationToken ct = default)
    {
        var entity = await GetAsync(subscriptionId, ct);
        string? jsonToTest = entity?.CredentialJson;

        if (string.IsNullOrWhiteSpace(jsonToTest))
            return (false, "لا توجد بيانات Firebase لهذا الاشتراك. احفظ ملف JSON أولاً.");

        return await TestJsonAsync(jsonToTest, ct);
    }

    public async Task<(bool Ok, string? Message)> TestJsonAsync(string credentialJson, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(credentialJson))
            return (false, "محتوى JSON مطلوب.");

        var validation = ValidateCredentialJson(credentialJson, out var projectId, out _);
        if (validation != null) return (false, validation);

        try
        {
            var credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromJson(credentialJson);
            if (credential.IsCreateScopedRequired)
                credential = credential.CreateScoped(["https://www.googleapis.com/auth/firebase.remoteconfig", "https://www.googleapis.com/auth/cloud-platform"]);

            var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: ct);
            if (string.IsNullOrWhiteSpace(token))
                return (false, "فشل الحصول على Access Token — تحقق من private_key.");

            var client = httpFactory.CreateClient("FirebaseRemoteConfig");
            var url = $"https://firebaseremoteconfig.googleapis.com/v1/projects/{projectId}/remoteConfig";
            using var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            using var resp = await client.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (resp.IsSuccessStatusCode)
                return (true, $"متصل بنجاح. Project: {projectId} — Remote Config جاهز ({(resp.Headers.ETag?.Tag ?? "no etag")}).");

            if ((int)resp.StatusCode == 403)
                return (false, $"فشل الاختبار 403 — Service Account ليس لديه دور Firebase Remote Config Admin. التفاصيل: {Trunc(body)}");
            if ((int)resp.StatusCode == 404)
                return (false, $"فشل الاختبار 404 — المشروع {projectId} غير موجود. {Trunc(body)}");
            return (false, $"فشل الاختبار {(int)resp.StatusCode}: {Trunc(body)}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TestConnection failed.");
            return (false, $"خطأ أثناء الاختبار: {ex.Message}");
        }
    }

    private static string? ValidateCredentialJson(string json, out string? projectId, out string? clientEmail)
    {
        projectId = null;
        clientEmail = null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var t) || t.GetString() != "service_account")
                return "JSON يجب أن يكون service_account (تحقق أنك نسخت ملف Service Account كاملاً).";
            if (!root.TryGetProperty("project_id", out var pid) || string.IsNullOrWhiteSpace(pid.GetString()))
                return "حقل project_id مفقود في JSON.";
            if (!root.TryGetProperty("private_key", out var pk) || string.IsNullOrWhiteSpace(pk.GetString()))
                return "حقل private_key مفقود.";
            if (!root.TryGetProperty("client_email", out var ce) || string.IsNullOrWhiteSpace(ce.GetString()))
                return "حقل client_email مفقود.";

            projectId = pid.GetString();
            clientEmail = ce.GetString();
            var pkStr = pk.GetString()!;
            if (!pkStr.Contains("BEGIN PRIVATE KEY"))
                return "private_key غير صالح (يجب أن يحتوي على BEGIN PRIVATE KEY).";
            return null;
        }
        catch (JsonException)
        {
            return "محتوى JSON غير صالح. تأكد أنك لصقت الملف كاملاً بين { ... }.";
        }
    }

    private static string MaskJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var pid = doc.RootElement.TryGetProperty("project_id", out var p) ? p.GetString() : "?";
            var email = doc.RootElement.TryGetProperty("client_email", out var e) ? e.GetString() : "?";
            var keyId = doc.RootElement.TryGetProperty("private_key_id", out var k) ? k.GetString() : "?";
            var shortKey = keyId != null && keyId.Length > 8 ? keyId[..8] + "..." : keyId;
            return $"project_id: {pid} | client_email: {email} | key_id: {shortKey}";
        }
        catch { return json.Length > 120 ? json[..120] + "..." : json; }
    }

    private static string Trunc(string s) => s.Length > 400 ? s[..400] : s;
}
