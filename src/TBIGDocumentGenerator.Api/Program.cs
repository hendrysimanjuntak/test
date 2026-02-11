using System.Reflection;
using TBIGDocumentGenerator.Application.DependencyInjection;
using TBIGDocumentGenerator.Application.Interfaces.Entities;
using TBIGDocumentGenerator.Application.Services.Entities;
using TBIGDocumentGenerator.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddInfrastructure();

var appAssembly = Assembly.Load("TBIGDocumentGenerator.Application");
builder.Services.AddServicesByConvention(appAssembly, "TBIGDocumentGenerator.Application.Services");
builder.Services.AddScoped(typeof(IBaseService<>), typeof(BaseService<>));



builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
