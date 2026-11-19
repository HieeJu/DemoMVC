using System.Collections.Concurrent;
using DemoMVC.Models;

namespace DemoMVC.Services;

public sealed class ExcelImportPlan
{
    public string EntityName { get; init; } = string.Empty;
    public List<Student> Students { get; init; } = [];
    public List<Faculty> Faculties { get; init; } = [];
    public List<ExcelImportRowViewModel> Rows { get; init; } = [];
}

public sealed class ExcelImportPreviewStore
{
    private readonly ConcurrentDictionary<Guid, (ExcelImportPlan Plan, DateTimeOffset ExpiresAt)> _plans = new();

    public Guid Store(ExcelImportPlan plan)
    {
        RemoveExpired();
        var token = Guid.NewGuid();
        _plans[token] = (plan, DateTimeOffset.UtcNow.AddMinutes(20));
        return token;
    }

    public bool TryTake(Guid token, out ExcelImportPlan? plan)
    {
        plan = null;
        if (!_plans.TryRemove(token, out var item) || item.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        plan = item.Plan;
        return true;
    }

    private void RemoveExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var item in _plans)
        {
            if (item.Value.ExpiresAt <= now)
            {
                _plans.TryRemove(item.Key, out _);
            }
        }
    }
}
