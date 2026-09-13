using Inventory.Api.Authentication;
using Inventory.Api.Contracts;
using Inventory.Application.Common.Models;
using Inventory.Application.InventoryMovements.Commands.RegisterInventoryMovement;
using Inventory.Application.InventoryMovements.Queries;
using Inventory.Application.InventoryMovements.Queries.GetInventoryMovements;
using Inventory.Application.InventoryMovements.Queries.GetMovementById;
using Inventory.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Inventory.Api.Controllers;
[ApiController]
[Route("api/inventory/movements")]
[Produces("application/json")]
public sealed class InventoryMovementsController : ControllerBase
{
    private readonly ISender _sender;
    public InventoryMovementsController(ISender sender)
    {
        _sender = sender;
    }
    [Authorize(Policy = AuthorizationPolicies.Read)]
    [HttpGet]
    [ProducesResponseType<PagedResult<MovementDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MovementDto>>> GetMovements(
        [FromQuery] int? productId,
        [FromQuery] MovementType? type,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        GetInventoryMovementsQuery query = new(productId, type, from, to, page, pageSize);

        return Ok(await _sender.Send(query, cancellationToken));
    }
    [Authorize(Policy = AuthorizationPolicies.Read)]
    [HttpGet("{id:int}")]
    [ProducesResponseType<MovementDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovementDto>> GetMovementById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetMovementByIdQuery(id), cancellationToken));
    }
    [Authorize(Policy = AuthorizationPolicies.Write)]
    [HttpPost]
    [ProducesResponseType<MovementDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MovementDto>> RegisterMovement(RegisterMovementRequest request, CancellationToken cancellationToken)
    {
        RegisterInventoryMovementCommand command = new(request.ProductId, request.Type, request.Quantity, request.Reason);
        int movementId = await _sender.Send(command, cancellationToken);
        MovementDto movement = await _sender.Send(new GetMovementByIdQuery(movementId), cancellationToken);

        return CreatedAtAction(nameof(GetMovementById), new { id = movementId }, movement);
    }
}
