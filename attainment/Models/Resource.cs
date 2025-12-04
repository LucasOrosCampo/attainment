using System;
using System.ComponentModel.DataAnnotations;

namespace attainment.Models;

/// <summary>
/// Represents a learning resource associated with a subject
/// </summary>
public class Resource
{
    [Key]
    public int Id { get; set; }
    
    [Required, MaxLength(100)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(1000)]
    public string? Url { get; set; }
    
    [MaxLength(255)]
    public string? FilePath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime? LastAccessed { get; set; }
    
    public bool IsFavorite { get; set; } = false;
    
    // Foreign key
    public int SubjectId { get; set; }
    
    // Navigation property
    public virtual Subject Subject { get; set; } = null!;
}
