using DataIntegration.Application.Services.Modules.MessageBroker;
using System.Reflection;
using TBIGDocumentGenerator.Application.DependencyInjection;
using TBIGDocumentGenerator.Application.Interfaces.Handlers;
using TBIGDocumentGenerator.Application.Services.Handlers;
using TBIGDocumentGenerator.Infrastructure.Data;
using TBIGDocumentGenerator.Infrastructure.Interfaces;
using TBIGDocumentGenerator.Infrastructure.Repositories;
using TBIGDocumentGenerator.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton<ICoreDbContextFactory, CoreDbContextFactory>();
builder.Services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

var appAssembly = Assembly.Load("TBIGDocumentGenerator.Application");
builder.Services.AddServicesWorkerByConvention(appAssembly, "TBIGDocumentGenerator.Application.Services");

//RabbitMQ
builder.Services.AddHostedService<RabbitMqConsumer>();

builder.Services.AddSingleton<IDictionary<string, IMessageHandler>>(sp => new Dictionary<string, IMessageHandler>
{
    { "documentgenerator.batch.pdf", sp.GetRequiredService<PdfHandler>() }
});

var host = builder.Build();
host.Run();
