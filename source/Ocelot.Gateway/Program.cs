using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var host = builder.Host;
var services = builder.Services;
var logging = builder.Logging;

// SERVICES
services.AddHealthChecks();
services.AddControllers();
services.AddOcelot();

// CONFIGURATION
configuration.AddJsonFile("ocelot.json");

// HOST
host.UseSerilog((host, services, logging) =>
{
	logging
		.MinimumLevel.Information()
		.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
		.MinimumLevel.Override("System", LogEventLevel.Warning)
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
		.Enrich.WithThreadId()
		.Enrich.WithProcessId()
		.Enrich.WithClientIp()
		.Enrich.WithExceptionDetails()
		.Enrich.FromLogContext();
});

// LOGGING
logging.ClearProviders();
logging.AddSerilog();

// MIDDLEWARE
var app = builder.Build();
app.UseSerilogRequestLogging(options =>
{
	options.GetLevel = (httpContext, elapsed, ex) => ex != null ?
		LogEventLevel.Error :
		LogEventLevel.Information;
});
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
		await context.Response.WriteAsync("Ocelot Gateway is running!"); // Sample endpoint
	});
});

// Use Ocelot as middleware
await app.UseOcelot();

// Run the application
await app.RunAsync();