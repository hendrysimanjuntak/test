using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBIGDocumentGenerator.Application.Request.PDF
{
    public class PdfConfigOverride
    {
        public string? PaperFormat { get; set; } = "A4";
        public bool? Landscape { get; set; } = false;
        public string MarginTop { get; set; } = "20mm";
        public string MarginBottom { get; set; } = "20mm";
        public string MarginLeft { get; set; } = "15mm";
        public string MarginRight { get; set; } = "15mm";
        public string? HeaderHtml { get; set; }
        public string? FooterHtml { get; set; }
    }
}
