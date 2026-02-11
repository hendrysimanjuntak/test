using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TBIGDocumentGenerator.Application.Interfaces.Modules;
using TBIGDocumentGenerator.Application.Request.Excel;

namespace TBIGDocumentGenerator.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExcelController : ControllerBase
    {
        private readonly IExcelService _excelService;

        public ExcelController(IExcelService excelService)
        {
            _excelService = excelService;
        }

        [HttpPost("Generate")]
        public async Task<IActionResult> Generate([FromBody] GenerateExcelRequest request)
        {
            try
            {
                var excelBytes = await _excelService.GenerateExcelAsync(request);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Excel_{request.ReportCode}_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
