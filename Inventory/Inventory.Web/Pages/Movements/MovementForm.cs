using System.ComponentModel.DataAnnotations;
using Inventory.Web.ApiClient;
namespace Inventory.Web.Pages.Movements;
public sealed class MovementForm
{
    [Required(ErrorMessage = "Choose a product.")]
    [Display(Name = "Product")]
    public int? ProductId { get; set; }
    [Required(ErrorMessage = "Choose whether units come in or go out.")]
    public MovementType? Type { get; set; }
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int? Quantity { get; set; }
    [StringLength(250)]
    public string? Reason { get; set; }
}
