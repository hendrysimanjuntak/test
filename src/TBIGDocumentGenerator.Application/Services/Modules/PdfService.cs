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

        public async Task<byte[]> GeneratePdfAsync(GeneratePdfRequest request)
        {
            var browserWsEndpoint = _configuration["Puppeteer:BrowserUri"]
                                    ?? "ws://localhost:3000";

            var connectOptions = new ConnectOptions
            {
                BrowserWSEndpoint = browserWsEndpoint
            };

            //// 2. Connect ke Remote Browser (Sidecar)
            //using var browser = await Puppeteer.ConnectAsync(connectOptions);
            //using var page = await browser.NewPageAsync();

            //// 3. Render Template 
            //var htmlContent = await RenderHtmlAsync(request);

            //await page.SetContentAsync(htmlContent, new NavigationOptions
            //{
            //    WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            //});

            //// 4. PDF Options Configuration
            //var pdfOptions = new PdfOptions
            //{
            //    PrintBackground = true,
            //    Format = ParsePaperFormat(request.Config?.PaperFormat),
            //    Landscape = request.Config?.Landscape ?? false,
            //    MarginOptions = new MarginOptions
            //    {
            //        Top = request.Config?.MarginTop ?? "20mm",
            //        Bottom = request.Config?.MarginBottom ?? "20mm",
            //        Left = "15mm",
            //        Right = "15mm"
            //    }
            //};

            //// 5. Generate
            //return await page.PdfDataAsync(pdfOptions);
            var template = (await _pdfTemplateService.GetListAsync(filter: w => w.Code == request.TemplateCode).ConfigureAwait(false)).FirstOrDefault();
            if (template == null) throw new Exception($"Template '{request.TemplateCode}' not found.");


            var renderContext = new
            {
                model = request.Data,
                server_date = DateTime.Now.ToString("dd MMM yyyy HH:mm"),
                server_user = "System",
                watermark = request.Config?.Watermark ?? template.Watermark
            };

            string? headerContent = null;
            if (!string.IsNullOrEmpty(template.HeaderHtml) || !string.IsNullOrEmpty(request.Config?.HeaderHtml))
            {
                var rawHeader = request.Config?.HeaderHtml ?? template.HeaderHtml;
                headerContent = await RenderHtmlAsync(rawHeader, renderContext);
            }

            string? footerContent = null;
            if (!string.IsNullOrEmpty(template.FooterHtml) || !string.IsNullOrEmpty(request.Config?.FooterHtml))
            {
                var rawFooter = request.Config?.FooterHtml ?? template.FooterHtml;
                footerContent = await RenderHtmlAsync(rawFooter, renderContext);
            }

            // 2. Logic Prioritas Config: Request > DB > Default Hardcoded
            var paperFormat = request.Config?.PaperFormat
                              ?? template.PaperFormat
                              ?? "A4";

            var isLandscape = request.Config?.Landscape
                              ?? template.Landscape
                              ?? false;

            var marginTop = request.Config?.MarginTop ?? template.MarginTop ?? "20mm";
            var marginBottom = request.Config?.MarginBottom ?? template.MarginBottom ?? "20mm";
            var marginLeft = request.Config?.MarginLeft ?? template.MarginLeft ?? "15mm";
            var marginRight = request.Config?.MarginRight ?? template.MarginRight ?? "15mm";

            // 3. Setup Options
            var pdfOptions = new PdfOptions
            {
                PrintBackground = true,
                Format = ParsePaperFormat(paperFormat),
                Landscape = isLandscape,
                MarginOptions = new MarginOptions
                {
                    Top = marginTop,
                    Bottom = marginBottom,
                    Left = marginLeft,
                    Right = marginRight
                },
                DisplayHeaderFooter = !string.IsNullOrEmpty(footerContent) || !string.IsNullOrEmpty(template.HeaderHtml),
                FooterTemplate = footerContent ?? "<div></div>",
                HeaderTemplate = template.HeaderHtml ?? "<div></div>"
            };

            // 4. Inject Header/Footer Template
            // Puppeteer butuh HTML yang valid dan style inline untuk header/footer
            //if (pdfOptions.DisplayHeaderFooter)
            //{
            //    pdfOptions.HeaderTemplate = template.HeaderHtml ?? "<div></div>";
            //    pdfOptions.FooterTemplate = await ParseFooterAsync(template.FooterHtml, request.Data) ?? "<div></div>";
            //}

            using var browser = await Puppeteer.ConnectAsync(connectOptions);
            using var page = await browser.NewPageAsync();

            // Render Body HTML
            var htmlContent = await RenderHtmlAsync(template.HtmlContent, renderContext);

            await page.SetContentAsync(htmlContent, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle0 } });

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

        private async Task<string> ParseFooterAsync(string? html, object data)
        {
            if (string.IsNullOrEmpty(html)) return null;
            var template = Template.Parse(html);
            return await template.RenderAsync(new { model = data });
        }

        private async Task<string> RenderHtmlAsync(string htmlContent, object request)
        {
            // Step A: Ambil Template dari Database berdasarkan CODE
            // Contoh request.TemplateCode = "INVOICE_V1"
            //var templateDto = (await _pdfTemplateService.GetListAsync(filter: w => w.Code == request.TemplateCode).ConfigureAwait(false)).FirstOrDefault();

            //if (templateDto == null)
            //{
            //    throw new Exception($"Template PDF dengan kode '{request.TemplateCode}' tidak ditemukan.");
            //}

            // Step B: Parse Template menggunakan Scriban
            var template = Template.Parse(htmlContent);

            if (template.HasErrors)
            {
                throw new Exception($"Scriban Error: {string.Join(", ", template.Messages)}");
            }

            // Step C: Gabungkan Template dengan Data (request.Data)
            // request.Data bisa berupa Object C# biasa atau JObject/JToken
            var result = await template.RenderAsync(request);

            return result;
        }
    }
}
