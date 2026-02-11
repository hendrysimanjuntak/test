using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TBIGDocumentGenerator.Domain.Models;
using TBIGDocumentGenerator.Domain.Models.Datatables;
using TBIGDocumentGenerator.Infrastructure.Data;

namespace TBIGDocumentGenerator.Application.Services.Entities.TBGAPPHFIRE141.TBIGDocumentGenerator
{
    public interface IBaseService<T> where T : class
    {
        Task<T> GetByPKAsync(params object[] keyValues);
        Task<T> GetByPKAsync(IUnitOfWork unitOfWork, params object[] keyValues);
        Task<IEnumerable<T>> GetListAsync(Expression<Func<T, bool>>? filter = null,
                                                Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
                                                string includeProperties = "");
        Task<IEnumerable<T>> GetListAsync(IUnitOfWork unitOfWork,
                                                Expression<Func<T, bool>>? filter = null,
                                                Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
                                                string includeProperties = "");
        IQueryable<T> GetListAsIQueryableAsync(IUnitOfWork unitOfWork, Expression<Func<T, bool>>? filter = null,
                                                Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
                                                string includeProperties = "");

        Task<int> GetCountAsync(Expression<Func<T, bool>>? filter = null);
        Task<int> GetCountAsync(IUnitOfWork unitOfWork, Expression<Func<T, bool>>? filter = null);
        Task<IPagedList<T>> GetPagedAsync(int pageIndex, int pageSize,
                                                Expression<Func<T, bool>>? filter = null,
                                                Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
                                                string includeProperties = "");
        Task<IPagedList<T>> GetPagedAsync(IUnitOfWork unitOfWork,
                                                int pageIndex, int pageSize,
                                                Expression<Func<T, bool>>? filter = null,
                                                Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
                                                string includeProperties = "");
        Task AddAsync(T entity);
        Task AddAsync(IUnitOfWork unitOfWork, T entity);
        Task AddBulkAsync(IEnumerable<T> entities);
        Task AddBulkAsync(IUnitOfWork unitOfWork, IEnumerable<T> entities);
        Task UpdateAsync(T entity);
        Task UpdateAsync(IUnitOfWork unitOfWork, T entity);
        Task UpdateBulkAsync(IEnumerable<T> entities);
        Task UpdateBulkAsync(IUnitOfWork unitOfWork, IEnumerable<T> entities);
        Task DeleteByPKAsync(params object[] keyValues);
        Task DeleteByPKAsync(IUnitOfWork unitOfWork, params object[] keyValues);
        Task DeleteByFilterAsync(Expression<Func<T, bool>> filter);
        Task DeleteByFilterAsync(IUnitOfWork unitOfWork, Expression<Func<T, bool>> filter);
        Task BulkInsertOrUpdateAsync(IEnumerable<T> entities);
        Task BulkInsertOrUpdateAsync(IUnitOfWork unitOfWork, IEnumerable<T> entities);
        Task BulkInsertOrUpdateAsync(IEnumerable<T> entities, IEnumerable<string> conflictColumns);
        Task BulkInsertOrUpdateAsync(IUnitOfWork unitOfWork, IEnumerable<T> entities, IEnumerable<string> conflictColumns);
        Task BulkSynchronizeAsync(IEnumerable<T> entities);
        Task BulkSynchronizeAsync(IUnitOfWork unitOfWork, IEnumerable<T> entities);
        Task BulkSynchronizeAsync(IEnumerable<T> entities, IEnumerable<string> conflictColumns);
        Task BulkSynchronizeAsync(IUnitOfWork unitOfWork, IEnumerable<T> entities, IEnumerable<string> conflictColumns);
        Task ExecuteStoredProcedureAsync(string procedureName, params SqlParameter[] parameters);

        IEnumerable<T> ExecuteStoredProcedureQuery<T>(
            string storedProcedureName,
            object parameters = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null);

        Task<IEnumerable<T>> ExecuteStoredProcedureQueryAsync(string procedureName, params SqlParameter[] parameters);
        //Task<(List<T> Data, int Total, int Filtered)> ExecutePagedStoredProcedureAsync<T>(
        //                                                                                string storedProcName,
        //                                                                                //Action<DbParameterCollection> setParameters)
        //                                                                                SqlParameter[] parameters)
        //                                                                                where T : class, new();

        Task<DataTablesResponse<T>> GetDataForDataTablesAsync<TRequest>(
            TRequest request,
            Func<TRequest, Expression<Func<T, bool>>?>? buildCustomFilter = null
        ) where TRequest : DataTablesRequest;

        Expression<Func<T, bool>>? CombineFiltersHelper(Expression<Func<T, bool>>? first, Expression<Func<T, bool>>? second);

        Task<IEnumerable<dynamic>> ExecuteDynamicQueryAsync(string sql, object param = null);
    }
}
