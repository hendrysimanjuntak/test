using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TBIGDocumentGenerator.Infrastructure.Data
{
    public class CoreDbContext : DbContext
    {
        public CoreDbContext(DbContextOptions<CoreDbContext> options) : base(options) { }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
			var domainAssembly = AppDomain.CurrentDomain.GetAssemblies()
							 .Where(w => w.GetName().Name == "TBIGDocumentGenerator.Domain")
							 .SelectMany(s => s.GetTypes())
							 .Where(t => t.IsClass && !t.IsAbstract && t.Namespace.StartsWith("TBIGDocumentGenerator.Domain.Entities"));

			if (domainAssembly != null)
			{
				foreach (var type in domainAssembly)
				{
					var entityBuilder = modelBuilder.Entity(type);
					
					// Menandai entitas keyless jika memiliki atribut [Keyless]
					if (type.GetCustomAttribute<KeylessAttribute>() != null)
					{
						entityBuilder.HasNoKey();
					}
					else
					{
						var keyProperties = type.GetProperties()
							.Where(p => p.GetCustomAttribute<KeyAttribute>() != null)
							.ToList();

						if (keyProperties.Any())
						{
							// Jika ada key yang ditentukan, gunakan key tersebut
							entityBuilder.HasKey(keyProperties.Select(p => p.Name).ToArray());
						}
						else
						{
							var idProperty = type.GetProperty("ID");
							if (idProperty != null)
							{
								entityBuilder.HasKey("ID");
							}
							else
							{
								// Jika tidak ada properti "Id", tentukan key lain atau lemparkan error
								throw new InvalidOperationException($"Entitas {type.Name} tidak memiliki primary key yang valid.");
							}
						}
					}
					
					// Ignore properties marked with [NotMapped] attribute
					var notMappedProperties = type.GetProperties()
						.Where(p => p.GetCustomAttribute<NotMappedAttribute>() != null);
					foreach (var prop in notMappedProperties)
					{
						entityBuilder.Ignore(prop.Name);
					}
				}
			}
			else
			{
				// Log warning atau handle jika assembly domain tidak ditemukan
				Console.WriteLine("WARNING: Assembly TBIGDocumentGenerator.Domain not found for model configuration.");
			}

			base.OnModelCreating(modelBuilder);

		}
    }
}
