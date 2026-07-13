using attainment.Models;
using Microsoft.EntityFrameworkCore;

namespace attainment.Application;

public interface ISettingsService
{
    Task<IReadOnlyList<Setting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task SaveAsync(IEnumerable<Setting> settings, CancellationToken cancellationToken = default);
}

public sealed class SettingsService(IDbContextFactory<ApplicationDbContext> dbFactory) : ISettingsService
{
    public async Task<IReadOnlyList<Setting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Settings
            .AsNoTracking()
            .OrderBy(setting => setting.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Settings
            .AsNoTracking()
            .Where(setting => setting.Key == key)
            .Select(setting => setting.Value)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(IEnumerable<Setting> settings, CancellationToken cancellationToken = default)
    {
        var values = settings.ToDictionary(setting => setting.Key, setting => setting.Value);
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);

        foreach (var key in SettingKeys.All())
        {
            var setting = await dbContext.Settings.FindAsync([key], cancellationToken);
            if (setting is null)
            {
                dbContext.Settings.Add(new Setting { Key = key, Value = values.GetValueOrDefault(key) });
            }
            else
            {
                setting.Value = values.GetValueOrDefault(key);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
