using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBIGDocumentGenerator.Infrastructure.Data
{
    public interface IUnitOfWorkFactory
    {
        IUnitOfWork Create(string connectionStringName = "TBIGSYSDB01_IDEANET");
    }


}
