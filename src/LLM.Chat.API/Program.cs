using LLM.Chat.API.Services;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using NLog;
using NLog.Web;

// Early init of NLog to allow startup and exception logging before host is built
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("LLM.Chat.API application starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // NLog: Setup NLog for Dependency injection
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // *************************************************************************
    // Set the API service name from the Aspire configuration
    string apiServiceName = "postgresqlmcpserver";
    // *************************************************************************

    builder.AddServiceDefaults();

    // Add services to the container.
    builder.Services.AddControllers();

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // Add configuration service
    builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

    // Add LiteDB database service
    builder.Services.AddSingleton<IChatDatabaseService>(sp =>
    {
        var env = sp.GetRequiredService<IWebHostEnvironment>();
        var dbPath = Path.Combine(env.ContentRootPath, "Data", "chat.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        logger.Info($"LiteDB database path: {dbPath}");
        return new ChatDatabaseService(dbPath);
    });

    // Add MCP Client
    builder.Services.AddSingleton<IMcpClient>(sp =>
    {
        McpClientOptions mcpClientOptions = new()
        { ClientInfo = new() { Name = "LLMChatAPI", Version = "1.0.0" } };

        var name = $"services__{apiServiceName}__https__0";
        var baseUrl = Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrEmpty(baseUrl))
        {
            logger.Error($"Environment variable '{name}' is not set. Make sure LLM.Chat.API is running through Aspire AppHost.");
            throw new InvalidOperationException($"Environment variable '{name}' is not set. The MCP server reference may not be configured correctly in the Aspire AppHost.");
        }

        var url = baseUrl + "/sse";

        logger.Info($"Environment variable '{name}' = {baseUrl}");
        logger.Info($"Connecting to MCP server at: {url}");

        SseClientTransportOptions sseTransportOptions = new()
        {
            Endpoint = new Uri(url)
        };

        SseClientTransport sseClientTransport = new(transportOptions: sseTransportOptions);

        var mcpClient = McpClientFactory.CreateAsync(
            sseClientTransport, mcpClientOptions).GetAwaiter().GetResult();

        logger.Info("MCP client initialized successfully");
        return mcpClient;
    });

    // Add Chat Client
    builder.Services.AddSingleton<IChatClient>(sp =>
    {
        var factory = new ChatClientFactory(
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<ILogger<ChatClientFactory>>());
        var client = factory.CreateChatClient();
        logger.Info("Chat client initialized successfully");
        return client;
    });

    var app = builder.Build();

    app.MapDefaultEndpoints();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        logger.Info("Swagger UI enabled at /swagger");
    }

    app.UseHttpsRedirection();

    app.UseCors();

    app.UseAuthorization();

    app.MapControllers();

    logger.Info("LLM.Chat.API application started successfully");
    app.Run();
}
catch (Exception exception)
{
    // NLog: catch setup errors
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit
    LogManager.Shutdown();
}
