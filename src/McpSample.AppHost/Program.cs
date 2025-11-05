var builder = DistributedApplication.CreateBuilder(args);

var aspnetsseserver = builder
    .AddProject<Projects.McpSample_AspNetCoreSseServer>("aspnetsseserver")
    .WithExternalHttpEndpoints();

var postgresqlmcpserver = builder
    .AddProject<Projects.McpSample_PostgreSQLMCPServer>("postgresqlmcpserver")
    .WithExternalHttpEndpoints();

var blazorchat = builder
    .AddProject<Projects.McpSample_BlazorChat>("blazorchat")
    .WithReference(aspnetsseserver)
    .WithReference(postgresqlmcpserver)
    .WithExternalHttpEndpoints();

var llmchatapi = builder
    .AddProject<Projects.LLM_Chat_API>("llm-chat-api")
    .WithReference(postgresqlmcpserver)
    .WithExternalHttpEndpoints();

builder.Build().Run();