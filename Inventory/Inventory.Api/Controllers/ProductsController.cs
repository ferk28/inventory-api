using Inventory.Api.Contracts;
using Inventory.Application.Common.Models;
using Inventory.Application.InventoryMovements.Queries;
using Inventory.Application.InventoryMovements.Queries.GetProductMovements;
using Inventory.Application.Products.Commands.CreateProduct;
using Inventory.Application.Products.Commands.DeleteProduct;
using Inventory.Application.Products.Commands.UpdateProduct;
using Inventory.Application.Products.Queries;
using Inventory.Application.Products.Queries.GetProductById;
using Inventory.Application.Products.Queries.GetProducts;
using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace Inventory.Api.Controllers;
[ApiController]
[Route("api/products")]
[Produces("application/json")]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;
    public ProductsController(ISender sender)
    {
        _sender = sender;
    }
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] int? categoryId,
        [FromQuery] string? search,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        GetProductsQuery query = new(categoryId, search, includeInactive, page, pageSize);

        return Ok(await _sender.Send(query, cancellationToken));
    }
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProductById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetProductByIdQuery(id), cancellationToken));
    }
    [HttpGet("{id:int}/movements")]
    [ProducesResponseType<PagedResult<MovementDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<MovementDto>>> GetProductMovements(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(new GetProductMovementsQuery(id, page, pageSize), cancellationToken));
    }
    [HttpPost]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductRequest request, CancellationToken cancellationToken)
    {
        CreateProductCommand command = new(request.Sku, request.Name, request.Description, request.Price, request.CategoryId);
        int productId = await _sender.Send(command, cancellationToken);
        ProductDto product = await _sender.Send(new GetProductByIdQuery(productId), cancellationToken);

        return CreatedAtAction(nameof(GetProductById), new { id = productId }, product);
    }
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProduct(int id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        UpdateProductCommand command = new(id, request.Name, request.Description, request.Price, request.CategoryId);
        await _sender.Send(command, cancellationToken);

        return NoContent();
    }
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteProductCommand(id), cancellationToken);

        return NoContent();
    }
}
