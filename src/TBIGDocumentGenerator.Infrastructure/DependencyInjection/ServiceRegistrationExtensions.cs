using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TBIGDocumentGenerator.Infrastructure.Data;
using TBIGDocumentGenerator.Infrastructure.Interfaces;
using TBIGDocumentGenerator.Infrastructure.Repositories;

namespace TBIGDocumentGenerator.Infrastructure.DependencyInjection
{
    public static class ServiceRegistrationExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<ICoreDbContextFactory, CoreDbContextFactory>();
            services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            return services;
        }
    }
}
