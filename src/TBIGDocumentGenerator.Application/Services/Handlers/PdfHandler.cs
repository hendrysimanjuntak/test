using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using TBIGDocumentGenerator.Application.Interfaces.Handlers;
using TBIGDocumentGenerator.Application.Interfaces.Modules;
using TBIGDocumentGenerator.Application.Request.PDF;

namespace TBIGDocumentGenerator.Application.Services.Handlers
{
    public class PdfHandler : IMessageHandler
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PdfHandler> _logger;
        public PdfHandler(ILogger<PdfHandler> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task HandleAsync(string message)
        {
            using var scope = _scopeFactory.CreateScope();
            var pdfService = scope.ServiceProvider.GetRequiredService<IPdfService>();

            try
            {
                // 1. Deserialize Payload (Batch)
                var batchRequest = JsonSerializer.Deserialize<BatchGeneratePdfRequest>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (batchRequest?.Documents == null || !batchRequest.Documents.Any())
                {
                    _logger.LogWarning("Empty Batch request or wrong format");
                    return;
                }

                _logger.LogInformation($"Processing Batch ID: {batchRequest.BatchId} with {batchRequest.Documents.Count} documents.");

                // 2. Loop every request in Array
                foreach (var docRequest in batchRequest.Documents)
                {
                    try
                    {
                        // A. Generate PDF (In-Memory)
                        // Use same logic service with API
                        byte[] pdfBytes = await pdfService.GeneratePdfAsync(docRequest);

                        // B. Path & Filename
                        string outputDir = docRequest.OutputDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedDocs");
                        string fileName = docRequest.FileName;

                        // Fallback filename if empty
                        if (string.IsNullOrWhiteSpace(fileName))
                        {
                            fileName = $"Doc_{Guid.NewGuid()}.pdf";
                        }

                        // Make sure .pdf extension already exists
                        if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) fileName += ".pdf";

                        // C. Create Directory if not exist
                        if (!Directory.Exists(outputDir))
                        {
                            Directory.CreateDirectory(outputDir);
                        }

                        // D. Save File to Disk (I/O)
                        string fullPath = Path.Combine(outputDir, fileName);
                        await File.WriteAllBytesAsync(fullPath, pdfBytes);

                        _logger.LogInformation($"[Success] Saved to: {fullPath}");
                    }
                    catch (Exception ex)
                    {
                        // Error handling per file (1 failed, resume others)
                        _logger.LogError(ex, $"[Failed] Gagal generate file: {docRequest.FileName ?? "Unknown"}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error processing batch message.");
            }
        }
    }
}
