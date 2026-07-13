using attainment.Models;
using Microsoft.EntityFrameworkCore;

namespace attainment.Application;

public interface IProductService
{
    Task<IReadOnlyList<Resource>> GetResourcesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Product> CreateAsync(
        string name,
        string content,
        int resourceId,
        ProductType type,
        CancellationToken cancellationToken = default);
}

public sealed class ProductService(IDbContextFactory<ApplicationDbContext> dbFactory) : IProductService
{
    public async Task<IReadOnlyList<Resource>> GetResourcesAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Resources
            .AsNoTracking()
            .OrderBy(resource => resource.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Products
            .AsNoTracking()
            .Include(product => product.Resource)
            .OrderByDescending(product => product.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product> CreateAsync(
        string name,
        string content,
        int resourceId,
        ProductType type,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (!await dbContext.Resources.AnyAsync(resource => resource.Id == resourceId, cancellationToken))
        {
            throw new ArgumentException("The selected resource does not exist.", nameof(resourceId));
        }

        if (await dbContext.Products.AnyAsync(product => product.Name == normalizedName, cancellationToken))
        {
            throw new DuplicateNameException("product", normalizedName);
        }

        var product = new Product
        {
            Name = normalizedName,
            Content = content.Trim(),
            ResourceId = resourceId,
            Type = type
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return product;
    }
}
