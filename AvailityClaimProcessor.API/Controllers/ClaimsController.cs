using AvailityClaimProcessor.Core.Models;
using AvailityClaimProcessor.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AvailityClaimProcessor.API.Controllers;

[ApiController]
[Route("api/claims")]
public class ClaimsController : ControllerBase
{
    private readonly ClaimProcessorDbContext _db;

    public ClaimsController(ClaimProcessorDbContext db)
    {
        _db = db;
    }

    /// <summary>GET /api/claims - List all claims with status</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllClaims([FromQuery] ClaimStatus? status = null)
    {
        var query = _db.ClaimResponses.AsQueryable();
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        var claims = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.ClaimId,
                c.Status,
                c.RawStatusCode,
                c.FileType,
                c.ServiceDate,
                c.SubmittedAmount,
                c.ApprovedAmount,
                c.PaidAmount,
                c.PatientResponsibility,
                c.CreatedAt
            })
            .ToListAsync();
        return Ok(claims);
    }

    /// <summary>GET /api/claims/{claimId}/status - Get claim status by claim ID</summary>
    [HttpGet("{claimId}/status")]
    public async Task<IActionResult> GetClaimStatus(string claimId)
    {
        var claim = await _db.ClaimResponses
            .Where(c => c.ClaimId == claimId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.ClaimId,
                c.Status,
                c.RawStatusCode,
                c.FileType,
                c.ServiceDate,
                c.SubmittedAmount,
                c.ApprovedAmount,
                c.PaidAmount,
                c.PatientResponsibility,
                c.RejectReasonCode,
                c.RejectReasonMessage,
                c.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return claim == null ? NotFound($"Claim '{claimId}' not found") : Ok(claim);
    }

    /// <summary>GET /api/claims/{claimId}/remittance - Get remittance details</summary>
    [HttpGet("{claimId}/remittance")]
    public async Task<IActionResult> GetRemittance(string claimId)
    {
        var remittances = await _db.RemittanceDetails
            .Where(r => r.ClaimId == claimId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return remittances.Count == 0 ? NotFound($"No remittance found for claim '{claimId}'") : Ok(remittances);
    }

    /// <summary>GET /api/claims/errors/{claimId} - Get processing errors</summary>
    [HttpGet("errors/{claimId}")]
    public async Task<IActionResult> GetErrors(string claimId)
    {
        var errors = await _db.ClaimResponses
            .Where(c => c.ClaimId == claimId)
            .SelectMany(c => c.ProcessingErrors)
            .ToListAsync();

        return Ok(errors);
    }
}
