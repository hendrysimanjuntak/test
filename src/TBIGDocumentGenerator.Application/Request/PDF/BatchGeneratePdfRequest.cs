using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBIGDocumentGenerator.Application.Request.PDF
{
    public class BatchGeneratePdfRequest
    {
        // Unique ID unik current batch (used for tracking log)
        public string BatchId { get; set; } = Guid.NewGuid().ToString();

        // List of document
        public List<GeneratePdfRequest> Documents { get; set; } = new();
    }
}
