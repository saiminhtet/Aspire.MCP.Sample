using McpSample.AspNetCoreSseServer;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;

var builder = WebApplication.CreateBuilder(args);

// Add default services
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();

// Register ISqlConnectionFactory and Tools for DI
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
//builder.Services.AddSingleton<Tools>();

// add MCP server
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<Tools>();
//.WithToolsFromAssembly();
var app = builder.Build();

// Initialize default endpoints
app.MapDefaultEndpoints();
app.UseHttpsRedirection();

// map endpoints
app.MapGet("/", () => $"Hello MCP Server! {DateTime.Now}");
app.MapMcp();

app.Run();
