using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace attainment.Models;

public record Product
{
   [Key] 
   public int Id { get; set; }
   
   public string Name { get; set; } = string.Empty;
   
   public string Content { get; set; } = string.Empty;
   
   public int ResourceId { get; set; }
   
   [ForeignKey(nameof(ResourceId))]
   public Resource Resource { get; set; } = null!;
   
   public ProductType Type { get; set; }
}

public enum ProductType
{
   Test, 
   Summary
}