using Microsoft.AspNetCore.Mvc;
namespace Inventory.Api.Controllers;
[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "healthy" });
    }
}
