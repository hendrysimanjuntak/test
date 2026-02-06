using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBIGDocumentGenerator.Infrastructure.Data
{
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly ICoreDbContextFactory _coreDbContextFactory;

        public UnitOfWorkFactory(ICoreDbContextFactory coreDbContextFactory)
        {
            _coreDbContextFactory = coreDbContextFactory;
        }

        public IUnitOfWork Create(string connectionStringName = "TBGAPPHFIRE141_TBIGDocumentGenerator")
        {
            return new UnitOfWork(_coreDbContextFactory, connectionStringName);
        }
    }
}
