using System.Windows;
using Microsoft.EntityFrameworkCore;
using attainment.Models;

namespace attainment;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize database if needed
        using var dbContext = new ApplicationDbContext();
        dbContext.Database.EnsureCreated();
        dbContext.Database.Migrate();

        EnsureSettingsExistAndCleanUp(dbContext);
        Seed(dbContext);

        dbContext.SaveChanges();
    }

    private static void EnsureSettingsExistAndCleanUp(ApplicationDbContext dbContext)
    {
        // Ensure settings keys exist in database
        foreach (var key in Models.KEYS.All())
        {
            var exists = dbContext.Settings.Find(key);
            if (exists == null)
            {
                dbContext.Settings.Add(new Models.Setting
                {
                    Key = key,
                    Value = null
                });
            }
        }

        // Remove any settings that are not declared in KEYS
        var allowed = new HashSet<string>(Models.KEYS.All());
        var toRemove = dbContext.Settings
            .Where(s => !allowed.Contains(s.Key))
            .ToList();
        if (toRemove.Count > 0)
        {
            dbContext.Settings.RemoveRange(toRemove);
        }
    }

    private static void Seed(ApplicationDbContext dbContext)
    {
        // Check if all tables are empty
        if (dbContext.Subjects.Any() || dbContext.Resources.Any() || dbContext.Products.Any()) return;
        // Create a subject
        var subject = new Models.Subject
        {
            Name = "Sample Subject",
            Description = "This is a sample subject for testing",
            IsFavorite = false
        };
        dbContext.Subjects.Add(subject);
        dbContext.SaveChanges(); // Save to get the generated Id

        // Create a resource linked to the subject
        var resource = new Models.Resource
        {
            Title = "Sample Resource",
            Description = "This is a sample resource linked to the subject",
            SubjectId = subject.Id,
            Subject = subject
        };
        dbContext.Resources.Add(resource);
        dbContext.SaveChanges(); // Save to get the generated Id

        // Create a product linked to the resource
        var product = new Product
        {
            Name = "Sample Product",
            Content = "This is sample product content",
            ResourceId = resource.Id,
            Resource = resource,
            Type = ProductType.Test
        };
        dbContext.Products.Add(product);
    }
    
}