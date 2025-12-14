using System;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace attainment;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost _host = null!;
    public static IServiceProvider Services { get; private set; } = null!;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((ctx, services) =>
            {
                services.AddDbContextFactory<ApplicationDbContext>(options =>
                {
                    options.UseSqlite($"Data Source={ApplicationDbContext.DatabasePath}");
                });

                // Infrastructure services
                services.AddTransient<Infrastructure.ExamRepository>();
                services.AddSingleton<Infrastructure.IAi, Infrastructure.OpenAi>();
                services.AddSingleton<Infrastructure.IPdf, Infrastructure.Pdf>();

                // ViewModels
                services.AddTransient<ViewModels.MainWindowViewModel>();
                services.AddTransient<ViewModels.ProductPageViewModel>();
                services.AddTransient<ViewModels.ExamCreationViewModel>();

                // Views
                services.AddTransient<MainWindow>();
                services.AddTransient<Views.ProductPage>();
                services.AddTransient<Views.ExamCreationPage>();
            })
            .Build();

        Services = _host.Services;

        // Apply migrations and seed data
        using (var scope = _host.Services.CreateScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            using var dbContext = factory.CreateDbContext();
            dbContext.Database.EnsureCreated();
            dbContext.Database.Migrate();
            EnsureSettingsExistAndCleanUp(dbContext);
            Seed(dbContext);
            dbContext.SaveChanges();
        }

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }

    internal static void EnsureSettingsExistAndCleanUp(ApplicationDbContext dbContext)
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
        var allowed = new System.Collections.Generic.HashSet<string>(Models.KEYS.All());
        var toRemove = dbContext.Settings
            .Where(s => !allowed.Contains(s.Key))
            .ToList();
        if (toRemove.Count > 0)
        {
            dbContext.Settings.RemoveRange(toRemove);
        }
    }

    internal static void Seed(ApplicationDbContext dbContext)
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
        var product = new Models.Product
        {
            Name = "Sample Product",
            Content = "This is sample product content",
            ResourceId = resource.Id,
            Resource = resource,
            Type = Models.ProductType.Exam
        };
        dbContext.Products.Add(product);
    }
}