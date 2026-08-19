using AvailityClaimResponseProcessor.Api.DTOs;
using AvailityClaimResponseProcessor.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AvailityClaimResponseProcessor.Api.Controllers;

[ApiController]
[Route("api/claims")]
public class ClaimsController : ControllerBase
{
    private readonly IClaimRepository _repository;
    private readonly ILogger<ClaimsController> _logger;

    public ClaimsController(IClaimRepository repository, ILogger<ClaimsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>GET /api/claims - List all claims with status.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClaimStatusDto>>> GetAll()
    {
        var claims = await _repository.GetAllAsync();
        return Ok(claims.Select(MapToDto));
    }

    /// <summary>GET /api/claims/{claimId}/status - Get claim status by claim ID.</summary>
    [HttpGet("{claimId}/status")]
    public async Task<ActionResult<ClaimStatusDto>> GetStatus(string claimId)
    {
        var claim = await _repository.GetByClaimIdAsync(claimId);
        if (claim is null)
        {
            // Sanitize user-supplied value before logging to prevent log-forging
            var safeClaimId = claimId.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
            _logger.LogInformation("Claim not found: {ClaimId}", safeClaimId);
            return NotFound(new { message = $"Claim '{safeClaimId}' not found." });
        }
        return Ok(MapToDto(claim));
    }

    /// <summary>GET /api/claims/{claimId}/remittance - Get remittance details.</summary>
    [HttpGet("{claimId}/remittance")]
    public async Task<ActionResult<RemittanceDto>> GetRemittance(string claimId)
    {
        var payment = await _repository.GetPaymentAsync(claimId);
        if (payment is null)
            return NotFound(new { message = $"No remittance found for claim '{claimId}'." });

        return Ok(new RemittanceDto(
            payment.ClaimId,
            payment.PaymentStatusCode,
            payment.SubmittedAmount,
            payment.ApprovedAmount,
            payment.PaidAmount,
            payment.PatientResponsibilityAmount,
            payment.CheckNumber,
            payment.PaymentDate
        ));
    }

    /// <summary>GET /api/claims/errors/{claimId} - Get processing errors.</summary>
    [HttpGet("errors/{claimId}")]
    public async Task<ActionResult<IEnumerable<ClaimErrorDto>>> GetErrors(string claimId)
    {
        var errors = await _repository.GetErrorsAsync(claimId);
        return Ok(errors.Select(e => new ClaimErrorDto(e.Id, e.ErrorCode, e.ErrorMessage, e.Segment, e.CreatedAt)));
    }

    private static ClaimStatusDto MapToDto(Core.Models.ClaimResponse c) => new(
        c.ClaimId, c.Status, c.StatusCode, c.StatusMessage,
        c.RejectionReasonCode, c.RejectionReasonMessage,
        c.FileType, c.SourceFileName,
        c.SubmittedAmount, c.ApprovedAmount, c.PaidAmount, c.PatientResponsibilityAmount,
        c.ProcessedAt, c.ServiceDate
    );
}
