using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBIGDocumentGenerator.Infrastructure.Data
{
    public interface ICoreDbContextFactory
    {
		CoreDbContext CreateDbContext(string connectionStringName);
	}

	public class CoreDbContextFactory : ICoreDbContextFactory
	{
		private readonly IConfiguration _configuration;

		public CoreDbContextFactory(IConfiguration configuration)
		{
			_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		}

		public CoreDbContext CreateDbContext(string connectionStringName)
		{
			var encryptedConnectionString = _configuration.GetConnectionString(connectionStringName);
			if (string.IsNullOrEmpty(encryptedConnectionString))
			{
				throw new ArgumentException($"Connection string '{connectionStringName}' not found or is empty in configuration.", nameof(connectionStringName));
			}

			string decryptedConnectionString;
			try
			{
				// TODO: Ganti CryptoHelper dengan implementasi yang aman (misalnya AES)
				decryptedConnectionString = CryptoHelper.Decrypt(encryptedConnectionString);
			}
			catch (Exception ex)
			{
				// Log error (opsional)
				throw new InvalidOperationException($"Failed to decrypt connection string '{connectionStringName}'.", ex);
			}

			var optionsBuilder = new DbContextOptionsBuilder<CoreDbContext>();
			optionsBuilder.UseSqlServer(decryptedConnectionString);

			// Pertimbangkan kembali penggunaan UseLazyLoadingProxies
			optionsBuilder.UseLazyLoadingProxies();

			return new CoreDbContext(optionsBuilder.Options);
		}
	}
}
