using attainment.Models;
using Microsoft.EntityFrameworkCore;

namespace attainment.Application;

public interface ISettingsService
{
    Task<IReadOnlyList<Setting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task SaveAsync(IEnumerable<Setting> settings, CancellationToken cancellationToken = default);
    Task ProtectSecretsAsync(CancellationToken cancellationToken = default);
}

public sealed class SettingsService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ISecretProtector secretProtector) : ISettingsService
{
    public async Task<IReadOnlyList<Setting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        var settings = await dbContext.Settings
            .AsNoTracking()
            .OrderBy(setting => setting.Key)
            .ToListAsync(cancellationToken);
        foreach (var setting in settings.Where(setting => setting.Key == SettingKeys.OpenAIKey && setting.Value is not null))
        {
            setting.Value = secretProtector.Unprotect(setting.Value!);
        }

        return settings;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        var value = await dbContext.Settings
            .AsNoTracking()
            .Where(setting => setting.Key == key)
            .Select(setting => setting.Value)
            .SingleOrDefaultAsync(cancellationToken);
        return key == SettingKeys.OpenAIKey && value is not null
            ? secretProtector.Unprotect(value)
            : value;
    }

    public async Task SaveAsync(IEnumerable<Setting> settings, CancellationToken cancellationToken = default)
    {
        var values = settings.ToDictionary(setting => setting.Key, setting => setting.Value);
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);

        foreach (var key in SettingKeys.All())
        {
            var value = values.GetValueOrDefault(key) ?? SettingKeys.DefaultValue(key);
            var setting = await dbContext.Settings.FindAsync([key], cancellationToken);
            if (setting is null)
            {
                dbContext.Settings.Add(new Setting { Key = key, Value = PrepareValue(key, value) });
            }
            else
            {
                setting.Value = PrepareValue(key, value);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ProtectSecretsAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        var setting = await dbContext.Settings.FindAsync([SettingKeys.OpenAIKey], cancellationToken);
        if (setting?.Value is { Length: > 0 } value && !secretProtector.IsProtected(value))
        {
            setting.Value = secretProtector.Protect(value);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private string? PrepareValue(string key, string? value)
    {
        return key == SettingKeys.OpenAIKey && !string.IsNullOrEmpty(value)
            ? secretProtector.Protect(value)
            : value;
    }
}
