using Carter;

using CleanTemplate.Api;
using CleanTemplate.Application;
using CleanTemplate.Infrastructure;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, config) => config
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.Console());

builder.Services
    .AddPresentation()
    .AddApplication()
    .AddInfrastructure();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapCarter();
app.MapHealthChecks("/health");

await app.RunAsync();

#pragma warning disable S1118
public partial class Program;
#pragma warning restore S1118