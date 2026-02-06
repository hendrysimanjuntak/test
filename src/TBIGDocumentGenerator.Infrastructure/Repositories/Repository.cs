
using EFCore.BulkExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TBIGDocumentGenerator.Domain.Models;
using System.Diagnostics;
using TBIGDocumentGenerator.Infrastructure.Data;
using TBIGDocumentGenerator.Infrastructure.Interfaces;

namespace TBIGDocumentGenerator.Infrastructure.Repositories
{
	public class Repository<T> : IRepository<T> where T : class
	{
		protected readonly CoreDbContext _context;
		protected readonly DbSet<T> _dbSet;

		public Repository(CoreDbContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
			_dbSet = _context.Set<T>();
		}

		//IQueryable
        public IQueryable<T> GetListQuery()
        {
            return _dbSet.AsQueryable(); // Mengembalikan DbSet sebagai IQueryable
        }
        public async Task<IEnumerable<T>> GetListAsync(IQueryable<T> query,
                                                        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
                                                        string includeProperties = "")
        {
            // Pastikan properti yang di-include diterapkan pada query yang diberikan
            foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty.Trim());
            }

            return orderBy != null ? await orderBy(query).ToListAsync() : await query.ToListAsync();
        }
        public async Task<IPagedList<T>> GetPagedAsync(IQueryable<T> query, int pageIndex, int pageSize,
                                                        Expression<Func<T, bool>>? filter = null,
                                                        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
                                                        string includeProperties = "")
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 10;

            // Jika ada filter tambahan, terapkan
            if (filter != null) { query = query.Where(filter); }

            // Terapkan include properties
            foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty.Trim());
            }

            var totalCount = await query.CountAsync();

            IQueryable<T> pagedQuery = query;
            if (orderBy != null)
            {
                pagedQuery = orderBy(query);
            }
            else
            {
                // Tambahkan default order jika tidak ada, penting untuk Skip/Take yang konsisten
                var idProperty = typeof(T).GetProperty("ID") ?? typeof(T).GetProperty("Id");
                if (idProperty != null)
                {
                    pagedQuery = pagedQuery.OrderBy(e => EF.Property<object>(e, idProperty.Name));
                }
                else
                {
                    throw new InvalidOperationException($"Cannot paginate entity {typeof(T).Name} without an 'ID' property or explicit orderBy function.");
                }
            }

            var items = await pagedQuery.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedList<T>(items, totalCount, pageIndex, pageSize);
        }
        public async Task<int> GetCountAsync(IQueryable<T> query, Expression<Func<T, bool>>? filter = null)
        {
            if (filter != null) { query = query.Where(filter); }
            return await query.CountAsync();
        }

        // Implementasi Table Operations
        public async Task<int> GetCountAsync(Expression<Func<T, bool>> filter = null)
		{
			return filter == null ?
				 await _dbSet.CountAsync() :
				 await _dbSet.CountAsync(filter);
		}

		public async Task<IEnumerable<T>> GetListAsync(Expression<Func<T, bool>> filter = null,
														 Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
														 string includeProperties = "")
		{
			IQueryable<T> query = _dbSet;

			if (filter != null) { query = query.Where(filter); }

			foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
			{
				query = query.Include(includeProperty.Trim()); 
			}

			return orderBy != null ? await orderBy(query).ToListAsync() : await query.ToListAsync();
		}

		public async Task<IPagedList<T>> GetPagedAsync(int pageIndex, int pageSize,
														 Expression<Func<T, bool>> filter = null,
														 Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
														 string includeProperties = "")
		{
			if (pageIndex < 1) pageIndex = 1;
			if (pageSize < 1) pageSize = 10; 

			IQueryable<T> query = _dbSet;

			if (filter != null) { query = query.Where(filter); }

			foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
			{
				query = query.Include(includeProperty.Trim());
			}

			var totalCount = await query.CountAsync(); 

			IQueryable<T> pagedQuery = query;
			if (orderBy != null)
			{
				pagedQuery = orderBy(query);
			}
			else
			{
				// Tambahkan default order jika tidak ada, penting untuk Skip/Take yang konsisten
				// Gantilah 'DefaultKeyProperty' dengan nama properti kunci atau properti lain yang sesuai
				 pagedQuery = pagedQuery.OrderBy(e => EF.Property<object>(e, "ID"));
				// Jika tidak ada properti kunci yang pasti, ini bisa jadi masalah.
				// Pertimbangkan mewajibkan orderBy untuk paginasi.
			}

			var items = await pagedQuery.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

			return new PagedList<T>(items, totalCount, pageIndex, pageSize); // Pastikan PagedList ada
		}


		public async Task<T> GetByPKAsync(params object[] keyValues)
		{
			return await _dbSet.FindAsync(keyValues);
		}

		public async Task AddAsync(T entity)
		{
			await _dbSet.AddAsync(entity);
		}

		// --- Modifikasi Bulk Operations ---

		public async Task AddBulkAsync(IEnumerable<T> entities)
		{
			var entityList = entities as List<T> ?? entities.ToList();
			var bulkConfig = new BulkConfig
			{
				// Di EFCore.BulkExtensions, KeepIdentity biasanya default false.
				// Jika kolom PK Anda adalah Identity dan Anda ingin DB meng-generate nilainya, set false (atau hilangkan baris ini).
				// Jika Anda menyediakan nilai PK sendiri (dan bukan identity), set true.
				// Opsi Z: InsertKeepIdentity = true -> Mungkin setara dengan PreserveInsertOrder = true dan SetOutputIdentity = true? Cek dokumentasi.
				// Asumsi paling umum: PK adalah identity, biarkan DB generate.
				// KeepIdentity = false, // (Default)
				PreserveInsertOrder = true, // Jika urutan penting
				SetOutputIdentity = true, // Jika ingin nilai identity di-update ke objek entity setelah insert
				BulkCopyTimeout = 180,
				BatchSize = 1000
			};
			await _context.BulkInsertAsync(entityList, bulkConfig);
		}

		public void Update(T entity)
		{
			// Cek apakah entity sudah dilacak
			var entry = _context.Entry(entity);
			if (entry.State == EntityState.Detached)
			{
				// Coba attach jika belum dilacak
				_dbSet.Attach(entity);
			}
			// Set state menjadi Modified
			entry.State = EntityState.Modified;
			// Perubahan BELUM disimpan ke DB sampai SaveChangesAsync/CommitAsync dipanggil
		}


		public async Task UpdateBulkAsync(IEnumerable<T> entities)
		{
			var entityList = entities as List<T> ?? entities.ToList();
			var bulkConfig = new BulkConfig
			{
				BulkCopyTimeout = 180,
				BatchSize = 1000,
				// EFCore.BulkExtensions secara default TIDAK mengupdate kolom Primary Key.
				// Jadi, opsi AllowUpdatePrimaryKeys = false dari Z sudah sesuai dengan default EFCore.BulkExtensions.
				// UpdateByProperties = // Opsional: Tentukan kolom mana yang digunakan untuk mencocokkan record (defaultnya PK)
				// PropertiesToInclude = // Opsional: Tentukan HANYA kolom yang ingin diupdate
			};
			await _context.BulkUpdateAsync(entityList, bulkConfig);
		}

		// Nama metode di EFCore.BulkExtensions adalah BulkInsertOrUpdateAsync
		public async Task BulkInsertOrUpdateAsync(IEnumerable<T> entities) // Ubah nama metode agar sesuai?
		{
			var entityList = entities as List<T> ?? entities.ToList();
			var bulkConfig = new BulkConfig
			{
				BatchSize = 1000,
				// KeepIdentity / PreserveInsertOrder / SetOutputIdentity seperti di AddBulkAsync jika relevan
				PreserveInsertOrder = true,
				SetOutputIdentity = true,
				BulkCopyTimeout = 180
				// UpdateByProperties = // Defaultnya menggunakan Primary Key untuk mencocokkan
			};
			// Gunakan BulkInsertOrUpdateAsync
			await _context.BulkInsertOrUpdateAsync(entityList, bulkConfig);
		}

		// Nama metode di EFCore.BulkExtensions adalah BulkInsertOrUpdateAsync
		public async Task BulkInsertOrUpdateAsync(IEnumerable<T> entities, IEnumerable<string> conflictColumns) // Ubah nama metode agar sesuai?
		{
			var entityList = entities as List<T> ?? entities.ToList();
			var conflictColumnList = conflictColumns?.ToList();

			var bulkConfig = new BulkConfig
			{
				BatchSize = 1000,
				PreserveInsertOrder = true,
				SetOutputIdentity = true,
				BulkCopyTimeout = 180
			};

			if (conflictColumnList != null || conflictColumnList.Any())
			{
				// Jika  ada conflict column, panggil versi dengan conflict column
				bulkConfig.UpdateByProperties = conflictColumnList;
			}

			// Gunakan BulkInsertOrUpdateAsync
			await _context.BulkInsertOrUpdateAsync(entityList, bulkConfig);
		}

		// Nama metode di EFCore.BulkExtensions adalah BulkInsertOrUpdateOrDeleteAsync
		public async Task BulkSynchronizeAsync(IEnumerable<T> entities)
		{
			var entityList = entities as List<T> ?? entities.ToList();
			var bulkConfig = new BulkConfig
			{
				BulkCopyTimeout = 180,
				BatchSize = 1000,
				// Konfigurasi lain jika perlu (misal: PreserveInsertOrder, SetOutputIdentity, dll.)
			};
			// Gunakan BulkInsertOrUpdateOrDeleteAsync
			await _context.BulkInsertOrUpdateOrDeleteAsync(entityList, bulkConfig);
		}

		// Nama metode di EFCore.BulkExtensions adalah BulkInsertOrUpdateOrDeleteAsync
		public async Task BulkSynchronizeAsync(IEnumerable<T> entities, IEnumerable<string> conflictColumns)
		{
			var entityList = entities as List<T> ?? entities.ToList();
			var conflictColumnList = conflictColumns?.ToList();

			if (conflictColumnList == null || !conflictColumnList.Any())
			{
				// Jika tidak ada conflict column, panggil versi tanpa conflict column
				await BulkSynchronizeAsync(entities);
				return;
			}

			var bulkConfig = new BulkConfig
			{
				UpdateByProperties = conflictColumnList, // Kolom untuk mencocokkan record
														 // Konfigurasi lain jika perlu
			};
			// Gunakan BulkInsertOrUpdateOrDeleteAsync
			await _context.BulkInsertOrUpdateOrDeleteAsync(entityList, bulkConfig);
		}

		public async Task DeleteByPKAsync(params object[] keyValues)
		{
			var entity = await _dbSet.FindAsync(keyValues);
			if (entity != null)
			{
				_dbSet.Remove(entity);
				// Perubahan BELUM disimpan ke DB sampai SaveChangesAsync/CommitAsync dipanggil
			}
		}

		public async Task DeleteByFilterAsync(Expression<Func<T, bool>> filter)
		{
			// Gunakan EFCore.BulkExtensions untuk delete yang lebih efisien
			// Ini langsung ke DB dan tidak melacak perubahan.
			await _dbSet.Where(filter).BatchDeleteAsync();

			// Opsi 1 (original - kurang efisien untuk banyak data):
			// var entities = await _dbSet.Where(filter).ToListAsync();
			// if (entities.Any())
			// {
			//  _dbSet.RemoveRange(entities);
			//  // Perubahan BELUM disimpan ke DB sampai SaveChangesAsync/CommitAsync dipanggil
			// }

			// Opsi 2: EF Core 7+ ExecuteDeleteAsync (jika pakai EF Core 7+)
			// await _dbSet.Where(filter).ExecuteDeleteAsync();
		}

		// --- Implementasi Stored Procedures (Tidak berubah) ---
		public async Task ExecuteStoredProcedureAsync(string procedureName, params SqlParameter[] parameters)
		{
			// Pastikan parameter TIDAK mengandung karakter jahat jika nama prosedur dinamis
			// Gunakan ExecuteSqlRawAsync untuk non-query
			var sql = $"EXEC {procedureName} {string.Join(", ", parameters.Select(p => p.ParameterName))}";
			await _context.Database.ExecuteSqlRawAsync(sql, parameters);
		}

		public async Task<IEnumerable<TResult>> ExecuteStoredProcedureQueryAsync<TResult>(string procedureName, params SqlParameter[] parameters) where TResult : class
		{
			// Pastikan TResult adalah tipe yang dikenal DbContext (bisa DbSet atau Keyless entity type)
			// Jika TResult bukan DbSet, konfigurasikan sebagai Keyless di OnModelCreating: modelBuilder.Entity<TResult>().HasNoKey();
			var sql = $"EXEC {procedureName} {string.Join(", ", parameters.Select(p => p.ParameterName))}";
			// Gunakan Set<TResult>().FromSqlRaw untuk memetakan hasil ke TResult
			return await _context.Set<TResult>().FromSqlRaw(sql, parameters).ToListAsync();
		}
	}
}