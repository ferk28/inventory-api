using Inventory.Api.Authentication;
using Inventory.Api.Contracts;
using Inventory.Application.Categories.Commands.CreateCategory;
using Inventory.Application.Categories.Commands.DeleteCategory;
using Inventory.Application.Categories.Commands.UpdateCategory;
using Inventory.Application.Categories.Queries;
using Inventory.Application.Categories.Queries.GetCategories;
using Inventory.Application.Categories.Queries.GetCategoryById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Inventory.Api.Controllers;
[Authorize]
[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ISender _sender;
    public CategoriesController(ISender sender)
    {
        _sender = sender;
    }
    [Authorize(Policy = AuthorizationPolicies.Read)]
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CategoryDto>>> GetCategories(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<CategoryDto> categories = await _sender.Send(new GetCategoriesQuery(includeInactive), cancellationToken);

        return Ok(categories);
    }
    [Authorize(Policy = AuthorizationPolicies.Read)]
    [HttpGet("{id:int}")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetCategoryById(int id, CancellationToken cancellationToken)
    {
        CategoryDto category = await _sender.Send(new GetCategoryByIdQuery(id), cancellationToken);

        return Ok(category);
    }
    [Authorize(Policy = AuthorizationPolicies.Write)]
    [HttpPost]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> CreateCategory(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        int categoryId = await _sender.Send(new CreateCategoryCommand(request.Name, request.Description), cancellationToken);
        CategoryDto category = await _sender.Send(new GetCategoryByIdQuery(categoryId), cancellationToken);

        return CreatedAtAction(nameof(GetCategoryById), new { id = categoryId }, category);
    }
    [Authorize(Policy = AuthorizationPolicies.Write)]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(int id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateCategoryCommand(id, request.Name, request.Description), cancellationToken);

        return NoContent();
    }
    [Authorize(Policy = AuthorizationPolicies.Write)]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteCategoryCommand(id), cancellationToken);

        return NoContent();
    }
}
