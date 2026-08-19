using AvailityClaimProcessor.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AvailityClaimProcessor.API.Controllers;

[ApiController]
[Route("api/benefits")]
public class BenefitsController : ControllerBase
{
    private readonly ClaimProcessorDbContext _db;

    public BenefitsController(ClaimProcessorDbContext db)
    {
        _db = db;
    }

    /// <summary>GET /api/benefits/{memberId} - Get benefit/eligibility information</summary>
    [HttpGet("{memberId}")]
    public async Task<IActionResult> GetBenefits(string memberId)
    {
        var benefits = await _db.BenefitRecords
            .Where(b => b.MemberId == memberId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return benefits.Count == 0 ? NotFound($"No benefits found for member '{memberId}'") : Ok(benefits);
    }
}
