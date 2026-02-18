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
        public string? PaperFormat { get; set; }
        public bool? Landscape { get; set; }
        public string? MarginTop { get; set; }
        public string? MarginBottom { get; set; }
        public string? MarginLeft { get; set; }
        public string? MarginRight { get; set; }
        public string? HeaderHtml { get; set; }
        public string? FooterHtml { get; set; }
        [MaxLength(100)]
        public string? Watermark { get; set; }
    }
}
