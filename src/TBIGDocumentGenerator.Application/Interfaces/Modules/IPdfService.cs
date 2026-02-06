using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBIGDocumentGenerator.Application.Request.PDF;

namespace TBIGDocumentGenerator.Application.Interfaces.Modules
{
    public interface IPdfService
    {
        Task<byte[]> GeneratePdfAsync(GeneratePdfRequest request);
    }
}
