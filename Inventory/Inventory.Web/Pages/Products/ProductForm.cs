using System.ComponentModel.DataAnnotations;
namespace Inventory.Web.Pages.Products;
// The limits mirror the API's validators, so most mistakes are caught in the browser before
// a request is sent. The API still validates everything; its answer is shown field by field.
public sealed class ProductForm
{
    [Required]
    [StringLength(50)]
    [Display(Name = "SKU")]
    public string Sku { get; set; } = string.Empty;
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;
    [StringLength(500)]
    public string? Description { get; set; }
    [Required]
    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Price must be zero or more.")]
    public decimal? Price { get; set; }
    [Required(ErrorMessage = "Choose a category.")]
    [Display(Name = "Category")]
    public int? CategoryId { get; set; }
}
