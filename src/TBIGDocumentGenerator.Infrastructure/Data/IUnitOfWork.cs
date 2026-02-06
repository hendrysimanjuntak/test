using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBIGDocumentGenerator.Infrastructure.Interfaces;

namespace TBIGDocumentGenerator.Infrastructure.Data
{
	public interface IUnitOfWork : IDisposable
	{
		CoreDbContext Context { get; }
		Task BeginTransactionAsync();
		Task CommitAsync(bool IsBulkOperation = false);
		Task RollbackAsync();
		IRepository<T> GetRepository<T>() where T : class;
		Task<int> SaveChangesAsync();
	}
}
