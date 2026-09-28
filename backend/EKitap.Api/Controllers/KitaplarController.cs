using EKitap.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EKitap.Api.Controllers;

[ApiController]
[Route("api/kitaplar")]
public class KitaplarController(IKitapService kitapService) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(220_000_000)]
    public async Task<IActionResult> Create([FromForm] string ad, [FromForm] List<IFormFile> files, CancellationToken cancellationToken)
    {
        try
        {
            var kitap = await kitapService.CreateAsync(ad, files, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = kitap.Id }, kitap);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var kitap = await kitapService.GetAsync(id, cancellationToken);
        return kitap is null ? NotFound(new { message = "Kitap bulunamadı." }) : Ok(kitap);
    }

    [HttpPost("{id:guid}/olustur")]
    public async Task<IActionResult> CreateBook(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var kitap = await kitapService.StartGenerationAsync(id, cancellationToken);
            return Accepted(kitap);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var pdf = await kitapService.GetPdfAsync(id, cancellationToken);
        if (pdf is null)
            return NotFound(new { message = "PDF henüz hazır değil." });

        Response.Headers.ContentDisposition = $"inline; filename=\"{pdf.Value.FileName}\"";
        return File(pdf.Value.Content, "application/pdf");
    }
}
