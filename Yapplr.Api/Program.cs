using Yapplr.Api.Extensions;
using Yapplr.Shared.Extensions;

// Auto-detect environment based on Git branch
EnvironmentExtensions.ConfigureEnvironmentFromGitBranch();

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

// Add services to the container
builder.Services.AddYapplrServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Handle command line arguments for admin operations
if (await app.HandleCommandLineArgumentsAsync(args))
{
    return;
}

// Run database migrations at startup
await app.RunDatabaseMigrationsAsync();

// Configure the HTTP request pipeline
app.ConfigureYapplrPipeline();

// Map endpoints
app.MapYapplrEndpoints();
app.MapYapplrHubs(builder.Configuration);
app.MapHealthCheck();

app.Run();
