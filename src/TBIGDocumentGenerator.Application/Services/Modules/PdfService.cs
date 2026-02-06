using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBIGDocumentGenerator.Application.Interfaces.Entities.TBGAPPHFIRE141.TBIGDocumentGenerator;
using TBIGDocumentGenerator.Application.Interfaces.Modules;
using TBIGDocumentGenerator.Application.Request.PDF;

namespace TBIGDocumentGenerator.Application.Services.Modules
{
    public class PdfService : IPdfService
    {
        private readonly IPdfTemplateService _templateService;

        public PdfService(IPdfTemplateService templateService)
        {
            _templateService = templateService;
        }

        public async Task<byte[]> GeneratePdfAsync(GeneratePdfRequest request)
        {
            // 1. Get Config from DB via Entity Service
            var template = (await _templateService.GetListAsync(filter: w => w.Code == request.TemplateCode).ConfigureAwait(false)).FirstOrDefault();

            // 2. Logic Generate (akan kita detailkan nanti)
            return null;
        }
    }
}
