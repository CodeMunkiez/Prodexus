using Prodexus.Functions.Data;
using Prodexus.Functions.Mapping;
using Prodexus.Functions.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")
        ?? builder.Configuration.GetConnectionString("SqlConnection")
        ?? "Server=(localdb)\\mssqllocaldb;Database=ProdexusDb;Trusted_Connection=True;";

    options.UseSqlServer(connectionString);
});

builder.Services.AddSingleton<IFlatFileParser, FlatFileParser>();
builder.Services.AddScoped<IRecordPersistor, RecordPersistor>();
builder.Services.AddSingleton<RowMapper>();

var host = builder.Build();
host.Run();

