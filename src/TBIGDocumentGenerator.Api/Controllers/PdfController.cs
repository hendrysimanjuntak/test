using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TBIGDocumentGenerator.Application.Interfaces.Modules;
using TBIGDocumentGenerator.Application.Request.PDF;

namespace TBIGDocumentGenerator.Api.Controllers
{ 
    [Route("api/[controller]")]
    [ApiController]
    public class PdfController : ControllerBase
    {
        private readonly IPdfService _pdfService;

        public PdfController(IPdfService pdfService)
        {
            _pdfService = pdfService;
        }

        [HttpPost("Generate")]
        public async Task<IActionResult> Generate([FromBody] GeneratePdfRequest request)
        {
            try
            {
                var pdfBytes = await _pdfService.GeneratePdfAsync(request);
                return File(pdfBytes, "application/pdf", $"Document_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
