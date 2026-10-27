using ContentFreeze.Models;
using ContentFreeze.Persistence;

namespace ContentFreeze.Tests.Support;

/// <summary>Stands in for the database, with the same lookups as <see cref="FreezeRepository"/>.</summary>
internal class InMemoryFreezeRepository : IFreezeRepository
{
    private readonly List<Freeze> _freezes = [];
    private readonly List<FreezeAuditEntry> _audit = [];

    public IReadOnlyList<Freeze> All => _freezes;

    public Freeze? GetCurrent(DateTime utcNow)
        => _freezes
            .Where(freeze => freeze.EndedAt is null && freeze.Cancelled is false && freeze.EndsAt > utcNow)
            .OrderBy(freeze => freeze.StartsAt)
            .FirstOrDefault();

    public Freeze? Get(int id) => _freezes.FirstOrDefault(freeze => freeze.Id == id);

    public Freeze? Find(string idOrCorrelationId)
        => _freezes.Where(freeze => freeze.CorrelationId == idOrCorrelationId).OrderByDescending(freeze => freeze.Id).FirstOrDefault()
            ?? (Guid.TryParse(idOrCorrelationId, out Guid key) ? _freezes.FirstOrDefault(freeze => freeze.Key == key) : null)
            ?? (int.TryParse(idOrCorrelationId, out var id) ? Get(id) : null);

    public IReadOnlyList<Freeze> GetRecent(int count) => _freezes.OrderByDescending(freeze => freeze.Id).Take(count).ToList();

    public IReadOnlyList<Freeze> GetPendingTransitions(DateTime utcNow)
        => _freezes
            .Where(freeze => freeze.Cancelled is false
                && ((freeze.StartNotifiedAt is null && freeze.StartsAt <= utcNow && freeze.EndedAt is null)
                    || (freeze.EndNotifiedAt is null && freeze.EndedAt is null && freeze.EndsAt <= utcNow)))
            .ToList();

    public Freeze Insert(Freeze freeze)
    {
        freeze.Id = _freezes.Count + 1;
        _freezes.Add(freeze);
        return freeze;
    }

    // Freezes are kept by reference, so updates are already in place.
    public void Update(Freeze freeze)
    {
    }

    public void AddBlockedAttempts(int freezeId, int count) => Get(freezeId)!.BlockedAttempts += count;

    public void AddAudit(FreezeAuditEntry entry) => _audit.Add(entry);

    public IReadOnlyList<FreezeAuditEntry> GetAudit(int freezeId) => _audit.Where(entry => entry.FreezeId == freezeId).ToList();
}
