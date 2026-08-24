using System.Collections.Concurrent;
using Dukan.Web.Application.DTOs.RemoteConfig;

namespace Dukan.Web.Infrastructure.Firebase;

/// <summary>
/// Singleton per-subscription draft store (upsert/delete) until Publish.
/// Each Subscription has its own Firebase project and thus its own draft.
/// </summary>
public sealed class RemoteConfigDraftStore
{
    private readonly ConcurrentDictionary<Guid, Draft> _store = new();

    public Draft Get(Guid subscriptionId) => _store.GetOrAdd(subscriptionId, _ => new Draft());

    public bool TryGet(Guid subscriptionId, out Draft? draft) => _store.TryGetValue(subscriptionId, out draft);

    public void Remove(Guid subscriptionId) => _store.TryRemove(subscriptionId, out _);

    public sealed class Draft
    {
        public SemaphoreSlim Lock { get; } = new(1, 1);

        public Dictionary<string, RemoteConfigParameterDto>? Parameters { get; set; }

        public HashSet<string> DeletedKeys { get; } = [];

        public string? ETag { get; set; }

        public bool HasChanges => (Parameters != null && Parameters.Count > 0) || DeletedKeys.Count > 0;

        public void Clear()
        {
            Parameters = null;
            DeletedKeys.Clear();
            ETag = null;
        }
    }
}
