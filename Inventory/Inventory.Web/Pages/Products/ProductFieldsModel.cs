using Microsoft.AspNetCore.Mvc.Rendering;
namespace Inventory.Web.Pages.Products;
public sealed record ProductFieldsModel(ProductForm Form, IReadOnlyList<SelectListItem> CategoryOptions, bool SkuIsEditable);
