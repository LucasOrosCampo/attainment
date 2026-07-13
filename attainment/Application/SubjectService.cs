using attainment.Models;
using Microsoft.EntityFrameworkCore;

namespace attainment.Application;

public sealed record SubjectDeletionImpact(int ResourceCount, int ProductCount);

public interface ISubjectService
{
    Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Subject> CreateAsync(string name, string? description, bool isFavorite, CancellationToken cancellationToken = default);
    Task<SubjectDeletionImpact> GetDeletionImpactAsync(int subjectId, CancellationToken cancellationToken = default);
    Task DeleteAsync(int subjectId, CancellationToken cancellationToken = default);
    Task SetFavoriteAsync(int subjectId, bool isFavorite, CancellationToken cancellationToken = default);
}

public sealed class SubjectService(IDbContextFactory<ApplicationDbContext> dbFactory) : ISubjectService
{
    public async Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Subjects
            .AsNoTracking()
            .Include(subject => subject.Resources)
            .OrderBy(subject => subject.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Subject> CreateAsync(
        string name,
        string? description,
        bool isFavorite,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Subject name is required.", nameof(name));
        }

        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (await dbContext.Subjects.AnyAsync(subject => subject.Name == normalizedName, cancellationToken))
        {
            throw new DuplicateNameException("subject", normalizedName);
        }

        var subject = new Subject
        {
            Name = normalizedName,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsFavorite = isFavorite
        };

        dbContext.Subjects.Add(subject);
        await dbContext.SaveChangesAsync(cancellationToken);
        return subject;
    }

    public async Task<SubjectDeletionImpact> GetDeletionImpactAsync(
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        var resourceCount = await dbContext.Resources.CountAsync(resource => resource.SubjectId == subjectId, cancellationToken);
        var productCount = await dbContext.Products.CountAsync(
            product => product.Resource.SubjectId == subjectId,
            cancellationToken);
        return new SubjectDeletionImpact(resourceCount, productCount);
    }

    public async Task DeleteAsync(int subjectId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        var subject = await dbContext.Subjects.FindAsync([subjectId], cancellationToken)
            ?? throw new InvalidOperationException("The subject no longer exists.");
        dbContext.Subjects.Remove(subject);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetFavoriteAsync(
        int subjectId,
        bool isFavorite,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        var subject = await dbContext.Subjects.FindAsync([subjectId], cancellationToken)
            ?? throw new InvalidOperationException("The subject no longer exists.");
        subject.IsFavorite = isFavorite;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
