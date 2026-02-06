using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBIGDocumentGenerator.Application.Interfaces.Entities.TBGAPPHFIRE141.TBIGDocumentGenerator;
using TBIGDocumentGenerator.Application.Services.Entities;
using TBIGDocumentGenerator.Domain.Entities.TBGAPPHFIRE141.TBIGDocumentGenerator;
using TBIGDocumentGenerator.Infrastructure.Data;

namespace TBIGDocumentGenerator.Application.Interfaces.Entities
{
    public class ExcelWorksheetService(IUnitOfWorkFactory unitOfWorkFactory, IServiceProvider serviceProvider) : BaseService<ExcelWorksheet>(unitOfWorkFactory, serviceProvider), IExcelWorksheetService
    {
    }
}
