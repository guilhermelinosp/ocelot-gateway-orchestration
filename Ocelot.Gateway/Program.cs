using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var host = builder.Host;
var services = builder.Services;

// SERVICES
services.AddHealthChecks();

services
	.AddOcelot()
	.AddPolly();

// CONFIGURATION
configuration.AddJsonFile("gateway.json");

// HOST
host.UseSerilog((host, services, logging) =>
{
	logging
		.MinimumLevel.Warning()
		.MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
		.MinimumLevel.Override("Serilog.AspNetCore.RequestLoggingMiddleware", LogEventLevel.Information)

		.WriteTo.Async(write =>
		{
			write.Console(
				outputTemplate:
				"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {ClientIp}  {ThreadId} {Message:lj} <p:{SourceContext}>{NewLine}{Exception}");
			write.File("logs/.log", rollingInterval: RollingInterval.Day,
				outputTemplate:
				"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {ClientIp}  {ThreadId} {Message:lj} <p:{SourceContext}>{NewLine}{Exception}");
		})
		.ReadFrom.Configuration(host.Configuration)
		.ReadFrom.Services(services)
		.Enrich.FromLogContext();
});
// MIDDLEWARE
var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapHealthChecks("/health", new HealthCheckOptions
{
	Predicate = _ => true,
	ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
// Map controllers
app.UseRouting();

app.UseEndpoints(endpoints =>
{
	endpoints.MapControllers(); // Map controllers

	endpoints.MapGet("/", async context =>
	{
		await context.Response.WriteAsync("Gateway is running!"); // Sample endpoint
	});
});


// Use Ocelot as middleware
await app.UseOcelot();

// Run the application
await app.RunAsync();