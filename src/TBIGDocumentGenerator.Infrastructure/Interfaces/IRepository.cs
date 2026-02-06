using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;
using TBIGDocumentGenerator.Domain.Models;

namespace TBIGDocumentGenerator.Infrastructure.Interfaces
{
	public interface IRepository<T> where T : class
	{
        IQueryable<T> GetListQuery();

        Task<int> GetCountAsync(IQueryable<T> query, Expression<Func<T, bool>> filter = null);

        Task<IEnumerable<T>> GetListAsync(IQueryable<T> query,
                                        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
                                        string includeProperties = "");

        Task<IPagedList<T>> GetPagedAsync(IQueryable<T> query, int pageIndex, int pageSize,
                                        Expression<Func<T, bool>> filter = null,
                                        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
                                        string includeProperties = "");
        // Table Operations
        Task<int> GetCountAsync(Expression<Func<T, bool>> filter = null);
		Task<IEnumerable<T>> GetListAsync(Expression<Func<T, bool>> filter = null,
											Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
											string includeProperties = "");
		Task<IPagedList<T>> GetPagedAsync(int pageIndex, int pageSize,
											Expression<Func<T, bool>> filter = null,
											Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
											string includeProperties = "");
		Task<T> GetByPKAsync(params object[] keyValues);
		Task AddAsync(T entity);
		Task AddBulkAsync(IEnumerable<T> entities);
		void Update(T entity);
		Task UpdateBulkAsync(IEnumerable<T> entities);

		Task BulkInsertOrUpdateAsync(IEnumerable<T> entities);
		Task BulkInsertOrUpdateAsync(IEnumerable<T> entities, IEnumerable<string> conflictColumns);
		Task BulkSynchronizeAsync(IEnumerable<T> entities);
		Task BulkSynchronizeAsync(IEnumerable<T> entities, IEnumerable<string> conflictColumns);

		Task DeleteByPKAsync(params object[] keyValues);
		Task DeleteByFilterAsync(Expression<Func<T, bool>> filter);

		Task ExecuteStoredProcedureAsync(string procedureName, params SqlParameter[] parameters);
		Task<IEnumerable<TResult>> ExecuteStoredProcedureQueryAsync<TResult>(string procedureName, params SqlParameter[] parameters) where TResult : class;
    }
}
