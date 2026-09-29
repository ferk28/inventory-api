using System.Net;
using FluentAssertions;
using Inventory.Web.ApiClient;
namespace Inventory.Web.Tests.ApiClient;
public sealed class InventoryApiClientTests
{
    private const string ProductJson = """
        {"id":7,"sku":"ELEC-0001","name":"Wireless Mouse","description":null,"price":19.99,"stock":25,
         "categoryId":1,"categoryName":"Electronics","isActive":true,"createdAt":"2026-09-29T09:15:30","updatedAt":null}
        """;
    [Fact]
    public async Task GetProductsAsync_SendsOnlyTheFiltersThatAreSet()
    {
        StubHttpMessageHandler handler = new(HttpStatusCode.OK, """{"items":[],"page":2,"pageSize":15,"totalCount":0}""");
        InventoryApiClient client = CreateClient(handler);
        await client.GetProductsAsync(new ProductFilter("  mouse ", null, false, 2, 15), CancellationToken.None);
        handler.Requests.Single().RequestUri!.PathAndQuery.Should().Be("/api/products?search=mouse&page=2&pageSize=15");
    }
    [Fact]
    public async Task GetProductsAsync_ReadsThePagedListFromTheApi()
    {
        StubHttpMessageHandler handler = new(HttpStatusCode.OK, $$"""{"items":[{{ProductJson}}],"page":1,"pageSize":15,"totalCount":31}""");
        InventoryApiClient client = CreateClient(handler);
        PagedList<ProductModel> products = await client.GetProductsAsync(new ProductFilter(null, null, false, 1, 15), CancellationToken.None);
        products.Items.Single().CategoryName.Should().Be("Electronics");
        products.TotalPages.Should().Be(3);
    }
    [Fact]
    public async Task GetMovementsAsync_ReadsTheMovementTypeByName()
    {
        const string body = """
            {"items":[{"id":1,"productId":7,"productSku":"ELEC-0001","type":"Out","quantity":3,"reason":null,
              "stockAfter":22,"createdAt":"2026-09-29T09:15:30"}],"page":1,"pageSize":20,"totalCount":1}
            """;
        InventoryApiClient client = CreateClient(new StubHttpMessageHandler(HttpStatusCode.OK, body));
        PagedList<MovementModel> movements = await client.GetMovementsAsync(
            new MovementFilter(null, null, null, null, 1, 20), CancellationToken.None);
        movements.Items.Single().Type.Should().Be(MovementType.Out);
    }
    [Fact]
    public async Task GetMovementsAsync_SendsDatesAsUtcWithoutAnOffset()
    {
        StubHttpMessageHandler handler = new(HttpStatusCode.OK, """{"items":[],"page":1,"pageSize":20,"totalCount":0}""");
        InventoryApiClient client = CreateClient(handler);
        DateTime from = new(2026, 9, 1, 6, 0, 0, DateTimeKind.Utc);
        await client.GetMovementsAsync(new MovementFilter(null, MovementType.In, from, null, 1, 20), CancellationToken.None);
        handler.Requests.Single().RequestUri!.Query.Should().Contain("type=In").And.Contain("from=2026-09-01T06%3A00%3A00.0000000&");
    }
    [Fact]
    public async Task CreateProductAsync_ReturnsTheProductFromTheCreatedResponse()
    {
        StubHttpMessageHandler handler = new(HttpStatusCode.Created, ProductJson);
        InventoryApiClient client = CreateClient(handler);
        ProductModel product = await client.CreateProductAsync(
            new CreateProductRequest("ELEC-0001", "Wireless Mouse", null, 19.99m, 1), CancellationToken.None);
        product.Id.Should().Be(7);
        handler.RequestBodies.Single().Should().Contain("\"sku\":\"ELEC-0001\"");
    }
    [Fact]
    public async Task CreateProductAsync_WithValidationProblem_ThrowsWithTheFieldErrors()
    {
        const string problem = """{"title":"Validation failed","status":400,"errors":{"Price":["must be zero or more"]}}""";
        InventoryApiClient client = CreateClient(new StubHttpMessageHandler(HttpStatusCode.BadRequest, problem, "application/problem+json"));
        Func<Task> create = () => client.CreateProductAsync(new CreateProductRequest("X", "Y", null, -1, 1), CancellationToken.None);
        ApiException exception = (await create.Should().ThrowAsync<ApiException>()).Which;
        exception.Errors["Price"].Should().ContainSingle("must be zero or more");
        exception.IsUserCorrectable.Should().BeTrue();
    }
    [Fact]
    public async Task RegisterMovementAsync_WithBusinessRuleViolation_ThrowsWithTheApiDetail()
    {
        const string problem = """{"title":"Business rule violation","status":422,"detail":"Product 1 has 3 units in stock, cannot remove 5."}""";
        InventoryApiClient client = CreateClient(new StubHttpMessageHandler(HttpStatusCode.UnprocessableEntity, problem, "application/problem+json"));
        Func<Task> register = () => client.RegisterMovementAsync(new RegisterMovementRequest(1, MovementType.Out, 5, null), CancellationToken.None);
        ApiException exception = (await register.Should().ThrowAsync<ApiException>()).Which;
        exception.Message.Should().Be("Product 1 has 3 units in stock, cannot remove 5.");
        exception.IsUserCorrectable.Should().BeTrue();
    }
    [Fact]
    public async Task GetProductAsync_WithUnauthorizedAndNoBody_ThrowsWithTheStatus()
    {
        InventoryApiClient client = CreateClient(new StubHttpMessageHandler(HttpStatusCode.Unauthorized));
        Func<Task> get = () => client.GetProductAsync(1, CancellationToken.None);
        ApiException exception = (await get.Should().ThrowAsync<ApiException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        exception.IsUserCorrectable.Should().BeFalse();
    }
    [Fact]
    public async Task GetProductAsync_WithHtmlErrorPage_ThrowsAGenericApiException()
    {
        InventoryApiClient client = CreateClient(new StubHttpMessageHandler(HttpStatusCode.BadGateway, "<html>Bad gateway</html>", "text/html"));
        Func<Task> get = () => client.GetProductAsync(1, CancellationToken.None);
        ApiException exception = (await get.Should().ThrowAsync<ApiException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        exception.Message.Should().Be("The inventory API could not complete the request.");
    }
    [Fact]
    public async Task DeleteCategoryAsync_WithConflict_ThrowsWithTheApiDetail()
    {
        const string problem = """{"title":"Conflict","status":409,"detail":"Category 1 still has active products and cannot be deleted."}""";
        InventoryApiClient client = CreateClient(new StubHttpMessageHandler(HttpStatusCode.Conflict, problem, "application/problem+json"));
        Func<Task> delete = () => client.DeleteCategoryAsync(1, CancellationToken.None);
        (await delete.Should().ThrowAsync<ApiException>()).Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
    private static InventoryApiClient CreateClient(StubHttpMessageHandler handler)
    {
        return new InventoryApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });
    }
}
