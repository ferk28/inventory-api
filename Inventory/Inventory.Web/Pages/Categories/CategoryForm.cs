using System.ComponentModel.DataAnnotations;
namespace Inventory.Web.Pages.Categories;
public sealed class CategoryForm
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    [StringLength(500)]
    public string? Description { get; set; }
}
