using AvailityClaimResponseProcessor.Api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AvailityClaimResponseProcessor.Api.Controllers;

[ApiController]
[Route("api")]
public class HealthController : ControllerBase
{
    /// <summary>GET /api/health - Health check endpoint.</summary>
    [HttpGet("health")]
    public ActionResult<HealthDto> GetHealth() =>
        Ok(new HealthDto("Healthy", DateTime.UtcNow, "1.0.0"));
}
