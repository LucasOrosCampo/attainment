using System.ComponentModel.DataAnnotations;

namespace attainment.Models;

/// <summary>
/// Represents a subject for study
/// </summary>
public class Subject
{
    [Key]
    public int Id { get; set; }
    
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? Description { get; set; }
    
    public bool IsFavorite { get; set; } = false;
    
    // Navigation property
    public virtual ICollection<Resource> Resources { get; set; } = new List<Resource>();
}
