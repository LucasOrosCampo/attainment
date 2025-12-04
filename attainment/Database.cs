using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using attainment.Models;

namespace attainment;

/// <summary>
/// Represents the database context for the application.
/// Extends the Entity Framework DbContext to provide access to the database.
/// </summary>
public class ApplicationDbContext : DbContext
{
    // DbSets for each entity in the database
    public DbSet<Subject> Subjects { get; set; } = null!;
    public DbSet<Resource> Resources { get; set; } = null!;

    /// <summary>
    /// Default constructor needed for migrations
    /// </summary>
    public ApplicationDbContext() { } 
    
    /// <summary>
    /// Constructor with options parameter to configure the context
    /// </summary>
    /// <param name="options">The options to be used by the DbContext</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { } 
    
    /// <summary>
    /// Gets the database directory path at the root of the user's system
    /// </summary>
    public static string DatabaseDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
        ".attainment");
    
    /// <summary>
    /// Gets the full path to the database file
    /// </summary>
    public static string DatabasePath => Path.Combine(DatabaseDirectory, "attainment.db");
    
    /// <summary>
    /// Configures the database connection
    /// </summary>
    /// <param name="optionsBuilder">The builder used to create or modify options for the context</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Ensure the database directory exists
            if (!Directory.Exists(DatabaseDirectory))
            {
                Directory.CreateDirectory(DatabaseDirectory);
            }
            
            // Use SQLite as the database provider with a database file in the .attainment folder
            optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
        }
    }

    /// <summary>
    /// Configures the database model
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure relationships
        modelBuilder.Entity<Resource>()
            .HasOne(r => r.Subject)
            .WithMany(s => s.Resources)
            .HasForeignKey(r => r.SubjectId);

        // Seed initial subjects
        modelBuilder.Entity<Subject>().HasData(
            new Subject { Id = 1, Name = "Mathematics", Description = "Math courses and materials", IsFavorite = false },
            new Subject { Id = 2, Name = "Computer Science", Description = "Programming and computer theory", IsFavorite = true },
            new Subject { Id = 3, Name = "Physics", Description = "Physical sciences and mechanics", IsFavorite = false },
            new Subject { Id = 4, Name = "Languages", Description = "Foreign language studies", IsFavorite = false }
        );
    }
}
