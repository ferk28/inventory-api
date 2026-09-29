namespace Inventory.Web.Pages.Shared;
// "page" is a reserved route value in Razor Pages, so paging travels as "pageNumber".
public sealed record PagerModel(int PageNumber, int TotalPages, int TotalCount, IReadOnlyDictionary<string, string?> RouteValues)
{
    public Dictionary<string, string> RouteFor(int pageNumber)
    {
        Dictionary<string, string> values = RouteValues
            .Where(pair => !string.IsNullOrEmpty(pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value!);
        values["pageNumber"] = pageNumber.ToString();

        return values;
    }
}
