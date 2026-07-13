using attainment.Models;
using Microsoft.EntityFrameworkCore;

namespace attainment.Application;

public interface IResourceService
{
    Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Resource>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Resource> CreateAsync(string title, int subjectId, string? filePath, CancellationToken cancellationToken = default);
}

public sealed class ResourceService(IDbContextFactory<ApplicationDbContext> dbFactory) : IResourceService
{
    public async Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Subjects
            .AsNoTracking()
            .OrderBy(subject => subject.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Resource>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Resources
            .AsNoTracking()
            .Include(resource => resource.Subject)
            .OrderByDescending(resource => resource.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Resource> CreateAsync(
        string title,
        int subjectId,
        string? filePath,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.Trim();
        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            throw new ArgumentException("Resource title is required.", nameof(title));
        }

        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (!await dbContext.Subjects.AnyAsync(subject => subject.Id == subjectId, cancellationToken))
        {
            throw new ArgumentException("The selected subject does not exist.", nameof(subjectId));
        }

        if (await dbContext.Resources.AnyAsync(resource => resource.Title == normalizedTitle, cancellationToken))
        {
            throw new DuplicateNameException("resource", normalizedTitle);
        }

        var resource = new Resource
        {
            Title = normalizedTitle,
            SubjectId = subjectId,
            FilePath = filePath,
            CreatedAt = DateTime.Now
        };

        dbContext.Resources.Add(resource);
        await dbContext.SaveChangesAsync(cancellationToken);
        return resource;
    }
}
