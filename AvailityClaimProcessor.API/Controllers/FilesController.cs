using AvailityClaimProcessor.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AvailityClaimProcessor.API.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly ClaimProcessorDbContext _db;

    public FilesController(ClaimProcessorDbContext db)
    {
        _db = db;
    }

    /// <summary>GET /api/files - List all processed EDI/EBR/EBT files</summary>
    [HttpGet]
    public async Task<IActionResult> GetFiles()
    {
        var files = await _db.EdiFiles
            .OrderByDescending(f => f.DownloadedAt)
            .Select(f => new
            {
                f.Id,
                f.FileName,
                f.FileType,
                f.FileSize,
                f.DownloadedAt,
                f.ProcessedAt,
                f.IsProcessed,
                f.ErrorMessage
            })
            .ToListAsync();
        return Ok(files);
    }
}
