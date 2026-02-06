using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBIGDocumentGenerator.Application.Request.PDF
{
    public class GeneratePdfRequest
    {
        public string TemplateCode { get; set; } // Required: Template Code

        // Dynamic Data for Scriban. 
        // Using JsonElement / object
        public object Data { get; set; }

        public PdfConfigOverride? Config { get; set; }
        public NotificationConfig? Notification { get; set; }
    }
}
