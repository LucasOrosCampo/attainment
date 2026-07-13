using attainment.Application;
using attainment.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace attainment.test;

public sealed class PersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly TestDbContextFactory _factory;

    public PersistenceTests()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _factory = new TestDbContextFactory(options);

        using var dbContext = _factory.CreateDbContext();
        dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task SubjectService_PersistsFavoriteAndResourceCount()
    {
        var subjectService = new SubjectService(_factory);
        var resourceService = new ResourceService(_factory);
        var subject = await subjectService.CreateAsync("Mathematics", "Numbers", false);

        await resourceService.CreateAsync("Algebra", subject.Id, null);
        await subjectService.SetFavoriteAsync(subject.Id, true);

        var saved = Assert.Single(await subjectService.GetAllAsync());
        Assert.True(saved.IsFavorite);
        Assert.Single(saved.Resources);
    }

    [Fact]
    public async Task SubjectService_RejectsCaseInsensitiveDuplicateName()
    {
        var subjectService = new SubjectService(_factory);
        await subjectService.CreateAsync("Physics", null, false);

        await Assert.ThrowsAsync<DuplicateNameException>(
            () => subjectService.CreateAsync("physics", null, false));
    }

    [Fact]
    public async Task SettingsService_SavesAndReloadsDeclaredValue()
    {
        var protector = new TestSecretProtector();
        var settingsService = new SettingsService(_factory, protector);
        await settingsService.SaveAsync(
        [
            new Setting { Key = SettingKeys.OpenAIKey, Value = "test-key" }
        ]);

        var settings = await settingsService.GetAllAsync();
        var setting = Assert.Single(settings, setting => setting.Key == SettingKeys.OpenAIKey);
        Assert.Equal(SettingKeys.OpenAIKey, setting.Key);
        Assert.Equal("test-key", setting.Value);

        await using var dbContext = _factory.CreateDbContext();
        var storedValue = await dbContext.Settings
            .Where(stored => stored.Key == SettingKeys.OpenAIKey)
            .Select(stored => stored.Value)
            .SingleAsync();
        Assert.Equal("protected:test-key", storedValue);
        Assert.Contains(settings, saved =>
            saved.Key == SettingKeys.OpenAIModel && saved.Value == SettingKeys.DefaultValue(SettingKeys.OpenAIModel));
    }

    [Fact]
    public async Task SettingsService_ProtectsExistingPlaintextSecret()
    {
        await using (var dbContext = _factory.CreateDbContext())
        {
            dbContext.Settings.Add(new Setting { Key = SettingKeys.OpenAIKey, Value = "legacy-key" });
            await dbContext.SaveChangesAsync();
        }

        var settingsService = new SettingsService(_factory, new TestSecretProtector());
        await settingsService.ProtectSecretsAsync();

        await using var verificationContext = _factory.CreateDbContext();
        var storedValue = await verificationContext.Settings
            .Where(setting => setting.Key == SettingKeys.OpenAIKey)
            .Select(setting => setting.Value)
            .SingleAsync();
        Assert.Equal("protected:legacy-key", storedValue);
        Assert.Equal("legacy-key", await settingsService.GetValueAsync(SettingKeys.OpenAIKey));
    }

    [Fact]
    public async Task SubjectService_ReportsAndCascadesDeletionImpact()
    {
        var subjectService = new SubjectService(_factory);
        var resourceService = new ResourceService(_factory);
        var productService = new ProductService(_factory);
        var subject = await subjectService.CreateAsync("Chemistry", null, false);
        var resource = await resourceService.CreateAsync("Atoms", subject.Id, null);
        await productService.CreateAsync("Atomic exam", "{}", resource.Id, ProductType.Exam);

        var impact = await subjectService.GetDeletionImpactAsync(subject.Id);
        Assert.Equal(1, impact.ResourceCount);
        Assert.Equal(1, impact.ProductCount);

        await subjectService.DeleteAsync(subject.Id);

        await using var dbContext = _factory.CreateDbContext();
        Assert.Empty(await dbContext.Subjects.ToListAsync());
        Assert.Empty(await dbContext.Resources.ToListAsync());
        Assert.Empty(await dbContext.Products.ToListAsync());
    }

    [Fact]
    public async Task Migrations_DeduplicateExistingNamesBeforeAddingUniqueIndexes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var dbContext = new ApplicationDbContext(options);
        var migrator = dbContext.Database.GetService<IMigrator>();

        await migrator.MigrateAsync("20251212194413_initialization");
        dbContext.Subjects.AddRange(
            new Subject { Name = "Biology" },
            new Subject { Name = "biology" });
        await dbContext.SaveChangesAsync();

        await migrator.MigrateAsync();

        var names = await dbContext.Subjects.AsNoTracking().Select(subject => subject.Name).ToListAsync();
        Assert.Equal(2, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(2, (await dbContext.Database.GetAppliedMigrationsAsync()).Count());
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        private const string Prefix = "protected:";

        public string Protect(string plaintext) => IsProtected(plaintext) ? plaintext : Prefix + plaintext;

        public string Unprotect(string protectedValue) =>
            IsProtected(protectedValue) ? protectedValue[Prefix.Length..] : protectedValue;

        public bool IsProtected(string value) => value.StartsWith(Prefix, StringComparison.Ordinal);
    }
}
