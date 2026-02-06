// IDEANET.Infrastructure.Data.UnitOfWork.cs
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBIGDocumentGenerator.Infrastructure.Interfaces;
using TBIGDocumentGenerator.Infrastructure.Repositories;

// using EFCore.BulkExtensions; // Tidak perlu di sini jika hanya memanggil SaveChangesAsync

namespace TBIGDocumentGenerator.Infrastructure.Data
{
	public class UnitOfWork : IUnitOfWork
	{
		private CoreDbContext _context;
		private IDbContextTransaction _transaction;
		private bool _disposed;
		private readonly ICoreDbContextFactory _contextFactory;
		private readonly string _connectionStringName;
		private Dictionary<Type, object> _repositories;
		public CoreDbContext Context => _context;

		public UnitOfWork(ICoreDbContextFactory contextFactory, string connectionStringName = "TBGAPPHFIRE141_TBIGDocumentGenerator")
		{
			_contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
			_connectionStringName = connectionStringName;
			_context = _contextFactory.CreateDbContext(_connectionStringName);
			_repositories = new Dictionary<Type, object>();
		}

		public async Task BeginTransactionAsync()
		{
			if (_transaction != null)
			{
				throw new InvalidOperationException("A transaction is already in progress.");
			}
			_transaction = await _context.Database.BeginTransactionAsync();
		}

		public async Task CommitAsync(bool IsBulkOperation = false)
		{
			try
			{
				await (IsBulkOperation ? _context.BulkSaveChangesAsync() : _context.SaveChangesAsync()); // Simpan perubahan non-bulk ke DB
				if (_transaction != null)
				{
					await _transaction.CommitAsync(); // Commit transaksi DB jika ada
				}
			}
			catch
			{
				await RollbackAsync(); // Rollback jika SaveChanges atau Commit gagal
				throw;
			}
			finally
			{
				// Dispose transaksi setelah commit atau rollback berhasil
				if (_transaction != null)
				{
					await _transaction.DisposeAsync();
					_transaction = null;
				}
			}
		}

		public async Task RollbackAsync()
		{
			if (_transaction != null)
			{
				try
				{
					await _transaction.RollbackAsync();
				}
				finally // Pastikan dispose terpanggil meskipun rollback error
				{
					await _transaction.DisposeAsync();
					_transaction = null;
				}
			}
			// Mungkin Anda ingin membersihkan state DbContext jika diperlukan setelah rollback
			_context.ChangeTracker.Clear(); // Opsional: hapus entitas yang terlacak
		}

		public async Task<int> SaveChangesAsync()
		{
			// Metode ini tetap berguna untuk operasi non-bulk
			return await _context.SaveChangesAsync();
		}

		public IRepository<T> GetRepository<T>() where T : class
		{
			if (_repositories.TryGetValue(typeof(T), out object repo))
			{
				return (IRepository<T>)repo;
			}
			var repository = new Repository<T>(_context); // Berikan context ke Repository
			_repositories.Add(typeof(T), repository);
			return repository;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					// Rollback transaksi yang belum di-commit saat dispose
					// Ini adalah praktik yang aman untuk mencegah transaksi menggantung
					_transaction?.Rollback(); // Coba rollback (sinkron)
					_transaction?.Dispose();

					_context?.Dispose();
				}
				_transaction = null;
				_context = null;
				_repositories = null;
			}
			_disposed = true;
		}

        public IDbConnection GetDbConnection()
        {
            return _context.Database.GetDbConnection();
        }

        ~UnitOfWork()
		{
			Dispose(false);
		}
	}
}