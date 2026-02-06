using Castle.Core.Configuration;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using Scriban;
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
        private readonly IPdfTemplateService _pdfTemplateService;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public PdfService(IPdfTemplateService pdfTemplateService, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _pdfTemplateService = pdfTemplateService;
            _configuration = configuration;
        }

        //public async Task<byte[]> GeneratePdfAsync(GeneratePdfRequest request)
        //{
        //    // 1. Get Config from DB via Entity Service
        //    var template = (await _templateService.GetListAsync(filter: w => w.Code == request.TemplateCode).ConfigureAwait(false)).FirstOrDefault();

        //    // 2. Logic Generate (akan kita detailkan nanti)
        //    return null;
        //}

        public async Task<byte[]> GeneratePdfAsync(GeneratePdfRequest request)
        {
            var browserWsEndpoint = _configuration["Puppeteer:BrowserUri"]
                                    ?? "ws://localhost:3000";

            var connectOptions = new ConnectOptions
            {
                BrowserWSEndpoint = browserWsEndpoint
            };

            // 2. Connect ke Remote Browser (Sidecar)
            using var browser = await Puppeteer.ConnectAsync(connectOptions);
            using var page = await browser.NewPageAsync();

            // 3. Render Template 
            var htmlContent = await RenderHtmlAsync(request);

            await page.SetContentAsync(htmlContent, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });

            // 4. PDF Options Configuration
            var pdfOptions = new PdfOptions
            {
                PrintBackground = true,
                Format = ParsePaperFormat(request.Config?.PaperFormat),
                Landscape = request.Config?.Landscape ?? false,
                MarginOptions = new MarginOptions
                {
                    Top = request.Config?.MarginTop ?? "20mm",
                    Bottom = request.Config?.MarginBottom ?? "20mm",
                    Left = "15mm",
                    Right = "15mm"
                }
            };

            // 5. Generate
            return await page.PdfDataAsync(pdfOptions);
        }

        // Helper private for parsing paper format
        private PaperFormat ParsePaperFormat(string format)
        {
            return (format?.ToUpper()) switch
            {
                "A3" => PaperFormat.A3,
                "A5" => PaperFormat.A5,
                "LEGAL" => PaperFormat.Legal,
                "LETTER" => PaperFormat.Letter,
                _ => PaperFormat.A4
            };
        }

        private async Task<string> RenderHtmlAsync(GeneratePdfRequest request)
        {
            // Step A: Ambil Template dari Database berdasarkan CODE
            // Contoh request.TemplateCode = "INVOICE_V1"
            var templateDto = (await _pdfTemplateService.GetListAsync(filter: w => w.Code == request.TemplateCode).ConfigureAwait(false)).FirstOrDefault();

            if (templateDto == null)
            {
                throw new Exception($"Template PDF dengan kode '{request.TemplateCode}' tidak ditemukan.");
            }

            // Step B: Parse Template menggunakan Scriban
            var template = Template.Parse(templateDto.HtmlContent);

            if (template.HasErrors)
            {
                throw new Exception($"Scriban Error: {string.Join(", ", template.Messages)}");
            }

            // Step C: Gabungkan Template dengan Data (request.Data)
            // request.Data bisa berupa Object C# biasa atau JObject/JToken
            var result = await template.RenderAsync(new { model = request.Data });

            return result;
        }
    }
}
