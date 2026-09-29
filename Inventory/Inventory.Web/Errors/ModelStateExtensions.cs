using Inventory.Web.ApiClient;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace Inventory.Web.Errors;
public static class ModelStateExtensions
{
    // The API names each validation error after the request property ("Sku", "Price"),
    // and the forms use the same names, so the message lands under the field it belongs to.
    // Conflicts and business rules have no field and go to the summary at the top.
    public static void AddApiErrors(this ModelStateDictionary modelState, ApiException exception, string prefix)
    {
        if (exception.Errors.Count == 0)
        {
            modelState.AddModelError(string.Empty, exception.Message);
            return;
        }
        foreach ((string field, string[] messages) in exception.Errors)
        {
            foreach (string message in messages)
            {
                modelState.AddModelError($"{prefix}.{field}", message);
            }
        }
    }
}
