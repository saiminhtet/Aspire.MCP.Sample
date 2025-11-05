# LLM.Chat.API

A production-ready ASP.NET Core Web API that provides RESTful endpoints for an AI chat service with **Model Context Protocol (MCP)** integration. Features LiteDB for lightweight data persistence and NLog for comprehensive logging.

---

## 📋 Table of Contents

- [Features](#features)
- [Architecture Overview](#architecture-overview)
- [Getting Started](#getting-started)
- [API Documentation](#api-documentation)
- [Configuration](#configuration)
- [Logging](#logging)
- [Database](#database)
- [Aspire Integration](#aspire-integration)
- [Development](#development)
- [Testing](#testing)
- [Deployment](#deployment)

---

## ✨ Features

- **6 RESTful API Endpoints** for comprehensive chat functionality
- **LiteDB** - Embedded NoSQL database for lightweight, file-based persistence
- **NLog** - Structured logging with file rotation and multiple targets
- **MCP Integration** - Model Context Protocol for tool-based AI interactions
- **Multi-Provider AI Support** - OpenAI, Azure AI, GitHub Models, Ollama
- **CORS Enabled** - Ready for frontend integration
- **Swagger/OpenAPI** - Interactive API documentation
- **.NET Aspire** - Cloud-native orchestration and service discovery
- **Dependency Injection** - Clean architecture with interface-based design

---

## 🏗️ Architecture Overview

```
LLM.Chat.API/
├── Controllers/
│   └── ChatController.cs              # REST API endpoints
├── Models/
│   ├── ApiResponse.cs                 # Standard response wrapper
│   ├── ChatMessage.cs                 # Message entity
│   ├── ChatSession.cs                 # Session entity
│   ├── McpServerStatus.cs             # MCP server status
│   └── SendMessageRequest.cs          # DTOs
├── Services/
│   ├── ChatDatabaseService.cs         # LiteDB data access layer
│   └── ChatClientFactory.cs           # AI client factory
├── Data/
│   └── chat.db                        # LiteDB database file (auto-created)
├── logs/                              # NLog log files (auto-created)
│   ├── llm-chat-api-all-YYYY-MM-DD.log
│   ├── llm-chat-api-own-YYYY-MM-DD.log
│   ├── llm-chat-api-error-YYYY-MM-DD.log
│   └── llm-chat-api-chat-YYYY-MM-DD.log
├── nlog.config                        # NLog configuration
├── appsettings.json                   # Application configuration
└── Program.cs                         # Application startup

```

---

## 🚀 Getting Started

### Prerequisites

- **.NET 9.0 SDK** or later
- **MCP Server** (e.g., postgresqlmcpserver) running
- **AI Provider** configured (OpenAI, Azure AI, or Ollama)

### Quick Start with Aspire

```bash
# Clone the repository
cd Aspire.MCP.Sample

# Run with .NET Aspire
dotnet run --project src/McpSample.AppHost
```

The API will be available through the Aspire dashboard. Access the dashboard to see all services including `llm-chat-api`.

### Standalone Execution

```bash
cd LLM.Chat.API

# Restore packages
dotnet restore

# Run the application
dotnet run
```

Access the API at:
- **Swagger UI**: `https://localhost:{port}/swagger`
- **API Base**: `https://localhost:{port}/api/chat`

---

## 📚 API Documentation

### Base URL

```
https://localhost:{port}/api/chat
```

### Response Format

All API responses follow this standard format:

```json
{
  "succeeded": true,
  "failed": false,
  "data": { /* response data */ },
  "message": "Optional error message"
}
```

---

### 1. Send Message

Send a message to the AI and receive a response.

**Endpoint:** `POST /api/chat/message`

**Request Body:**
```json
{
  "message": "What can you help me with?",
  "sessionId": "optional-existing-session-id",
  "context": {}
}
```

**Parameters:**
- `message` (string, required): The user's message
- `sessionId` (string, optional): Existing session ID. If omitted, a new session is created
- `context` (object, optional): Additional context for the conversation

**Response:** `200 OK`
```json
{
  "succeeded": true,
  "failed": false,
  "data": {
    "message": {
      "id": "msg-638675890123456789-assistant",
      "role": "assistant",
      "content": "I can help you with various tasks including...",
      "timestamp": "2025-11-05T12:00:00.0000000Z",
      "metadata": {
        "model": "ChatClientProxy",
        "tokensUsed": 0,
        "mcpToolsUsed": []
      }
    },
    "sessionId": "abc123-def456-ghi789",
    "suggestions": [
      "Tell me more",
      "Can you explain that differently?",
      "What else can you help with?"
    ]
  }
}
```

**Error Response:** `404 Not Found` (if sessionId doesn't exist)
```json
{
  "succeeded": false,
  "failed": true,
  "data": null,
  "message": "Session not found"
}
```

**cURL Example:**
```bash
curl -X POST "https://localhost:5001/api/chat/message" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Hello, how can you help me?",
    "sessionId": null
  }'
```

---

### 2. Get Session

Retrieve a specific chat session with complete conversation history.

**Endpoint:** `GET /api/chat/session/{id}`

**URL Parameters:**
- `id` (string, required): The session ID

**Response:** `200 OK`
```json
{
  "succeeded": true,
  "failed": false,
  "data": {
    "id": "abc123-def456-ghi789",
    "title": "Conversation about AI",
    "messages": [
      {
        "id": "msg-638675890123456789-user",
        "role": "user",
        "content": "Hello!",
        "timestamp": "2025-11-05T12:00:00.0000000Z",
        "metadata": null
      },
      {
        "id": "msg-638675890123456790-assistant",
        "role": "assistant",
        "content": "Hi! How can I help you?",
        "timestamp": "2025-11-05T12:00:01.0000000Z",
        "metadata": {
          "model": "ChatClientProxy",
          "tokensUsed": 0,
          "mcpToolsUsed": []
        }
      }
    ],
    "createdAt": "2025-11-05T12:00:00.0000000Z",
    "updatedAt": "2025-11-05T12:00:01.0000000Z"
  }
}
```

**Error Response:** `404 Not Found`
```json
{
  "succeeded": false,
  "failed": true,
  "data": null,
  "message": "Session not found"
}
```

**cURL Example:**
```bash
curl -X GET "https://localhost:5001/api/chat/session/abc123-def456-ghi789"
```

---

### 3. List All Sessions

Get all chat sessions, ordered by most recently updated.

**Endpoint:** `GET /api/chat/sessions`

**Response:** `200 OK`
```json
{
  "succeeded": true,
  "failed": false,
  "data": [
    {
      "id": "session-1",
      "title": "First Conversation",
      "messages": [...],
      "createdAt": "2025-11-05T12:00:00.0000000Z",
      "updatedAt": "2025-11-05T12:05:00.0000000Z"
    },
    {
      "id": "session-2",
      "title": "Second Conversation",
      "messages": [...],
      "createdAt": "2025-11-05T11:00:00.0000000Z",
      "updatedAt": "2025-11-05T11:30:00.0000000Z"
    }
  ]
}
```

**cURL Example:**
```bash
curl -X GET "https://localhost:5001/api/chat/sessions"
```

---

### 4. Create Session

Create a new chat session.

**Endpoint:** `POST /api/chat/session`

**Request Body:**
```json
{
  "title": "My New Chat Session"
}
```

**Parameters:**
- `title` (string, optional): Session title. Defaults to "New Chat"

**Response:** `200 OK`
```json
{
  "succeeded": true,
  "failed": false,
  "data": {
    "id": "new-session-id-123",
    "title": "My New Chat Session",
    "messages": [],
    "createdAt": "2025-11-05T12:00:00.0000000Z",
    "updatedAt": "2025-11-05T12:00:00.0000000Z"
  }
}
```

**cURL Example:**
```bash
curl -X POST "https://localhost:5001/api/chat/session" \
  -H "Content-Type: application/json" \
  -d '{"title": "Planning Discussion"}'
```

---

### 5. Delete Session

Delete a chat session and all its messages.

**Endpoint:** `DELETE /api/chat/session/{id}`

**URL Parameters:**
- `id` (string, required): The session ID to delete

**Response:** `200 OK`
```json
{
  "succeeded": true,
  "failed": false,
  "data": true
}
```

**Error Response:** `404 Not Found`
```json
{
  "succeeded": false,
  "failed": true,
  "data": false,
  "message": "Session not found"
}
```

**cURL Example:**
```bash
curl -X DELETE "https://localhost:5001/api/chat/session/abc123-def456-ghi789"
```

---

### 6. MCP Server Status

Check the connection status of the MCP server.

**Endpoint:** `GET /api/chat/mcp/status`

**Response:** `200 OK`
```json
{
  "succeeded": true,
  "failed": false,
  "data": [
    {
      "name": "MCP Server",
      "connected": true,
      "error": null
    }
  ]
}
```

**Error State Response:**
```json
{
  "succeeded": true,
  "failed": false,
  "data": [
    {
      "name": "MCP Server",
      "connected": false,
      "error": "Connection timeout"
    }
  ]
}
```

**cURL Example:**
```bash
curl -X GET "https://localhost:5001/api/chat/mcp/status"
```

---

## ⚙️ Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "endpoint": "http://localhost:11434",
  "apikey": "",
  "deploymentname": "llama3.2"
}
```

### AI Provider Configuration

#### 1. Ollama (Local)

```json
{
  "endpoint": "http://localhost:11434",
  "apikey": "",
  "deploymentname": "llama3.2"
}
```

#### 2. OpenAI API

```json
{
  "endpoint": "https://api.openai.com",
  "apikey": "sk-your-openai-api-key-here",
  "deploymentname": "gpt-4"
}
```

#### 3. Azure OpenAI

```json
{
  "endpoint": "https://your-resource.openai.azure.com",
  "apikey": "your-azure-api-key",
  "deploymentname": "gpt-4"
}
```

#### 4. GitHub Models

```json
{
  "endpoint": "https://models.inference.ai.azure.com",
  "apikey": "your-github-token",
  "deploymentname": "gpt-4o"
}
```

### Environment Variables

For production, use environment variables or user secrets:

```bash
export endpoint="https://api.openai.com"
export apikey="your-api-key"
export deploymentname="gpt-4"
```

Or use .NET User Secrets:

```bash
dotnet user-secrets set "apikey" "your-api-key"
dotnet user-secrets set "endpoint" "https://api.openai.com"
dotnet user-secrets set "deploymentname" "gpt-4"
```

---

## 📊 Logging

The application uses **NLog** for structured logging with multiple targets.

### Log Files

All log files are created in the `logs/` directory:

1. **llm-chat-api-all-{date}.log**
   - All application logs
   - Layout: `{timestamp}|{level}|{logger}|{message}`

2. **llm-chat-api-own-{date}.log**
   - Application-specific logs (excludes framework logs)
   - Includes request URLs and MVC actions

3. **llm-chat-api-error-{date}.log**
   - Error and Fatal level logs only
   - Includes stack traces and call sites

4. **llm-chat-api-chat-{date}.log**
   - Chat-specific logs from Controllers and Services
   - Useful for debugging chat flows

5. **internal-nlog.txt**
   - NLog internal diagnostics (if issues occur)

### Log Levels

- **Trace**: Detailed diagnostic information
- **Debug**: Debugging information
- **Info**: General informational messages
- **Warn**: Warning messages
- **Error**: Error events
- **Fatal**: Critical errors causing shutdown

### Configuration

Edit `nlog.config` to customize logging behavior:

```xml
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd">
  <targets>
    <!-- Add or modify targets -->
  </targets>
  <rules>
    <!-- Add or modify rules -->
  </rules>
</nlog>
```

### Viewing Logs

```bash
# View all logs
tail -f logs/llm-chat-api-all-*.log

# View errors only
tail -f logs/llm-chat-api-error-*.log

# View chat-specific logs
tail -f logs/llm-chat-api-chat-*.log
```

---

## 💾 Database

### LiteDB

LLM.Chat.API uses **LiteDB** - a serverless, embedded NoSQL database.

**Features:**
- Single file storage
- No external dependencies
- ACID transactions
- Automatic indexing
- Thread-safe operations

**Database Location:**
```
LLM.Chat.API/Data/chat.db
```

### Data Model

**ChatSession Collection:**
```json
{
  "_id": "session-guid",
  "Id": "session-guid",
  "Title": "Chat Title",
  "Messages": [
    {
      "Id": "msg-id",
      "Role": "user|assistant|tool",
      "Content": "Message content",
      "Timestamp": "ISO-8601 datetime",
      "Metadata": {
        "Model": "model-name",
        "TokensUsed": 123,
        "McpToolsUsed": ["tool1", "tool2"]
      }
    }
  ],
  "CreatedAt": "ISO-8601 datetime",
  "UpdatedAt": "ISO-8601 datetime"
}
```

### Database Operations

**Backup:**
```bash
cp LLM.Chat.API/Data/chat.db backup-chat-$(date +%Y%m%d).db
```

**Reset:**
```bash
rm LLM.Chat.API/Data/chat.db
# Database will be recreated on next startup
```

**Query (using LiteDB.Studio):**
1. Download [LiteDB.Studio](https://github.com/mbdavid/LiteDB.Studio)
2. Open `Data/chat.db`
3. Run queries:
```sql
SELECT * FROM sessions
SELECT $ FROM sessions WHERE $.Title LIKE '%chat%'
```

---

## 🌐 Aspire Integration

The API is fully integrated with **.NET Aspire** for cloud-native orchestration.

### Service Configuration

In `McpSample.AppHost/Program.cs`:

```csharp
var llmchatapi = builder
    .AddProject<Projects.LLM_Chat_API>("llm-chat-api")
    .WithReference(postgresqlmcpserver)
    .WithExternalHttpEndpoints();
```

### Service Discovery

The API automatically:
- Connects to MCP server via service discovery
- Registers with Aspire dashboard
- Provides health endpoints
- Exposes metrics

### Running with Aspire

```bash
dotnet run --project src/McpSample.AppHost
```

Access the Aspire dashboard to:
- View service status
- Monitor logs
- Check resource usage
- Manage deployments

---

## 🔧 Development

### Project Structure

```
LLM.Chat.API/
├── Controllers/         # API endpoints
├── Models/             # Data models and DTOs
├── Services/           # Business logic
├── Data/               # Database files
├── logs/               # Log files
└── Properties/         # Launch settings
```

### Adding New Endpoints

1. Define DTOs in `Models/`
2. Add service methods in `Services/`
3. Create controller action in `Controllers/ChatController.cs`
4. Update this README with documentation

### Code Style

- Use C# 12 features
- Follow .NET naming conventions
- Use async/await for I/O operations
- Log all important events
- Handle exceptions gracefully

### Building

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Clean and rebuild
dotnet clean && dotnet build
```

---

## 🧪 Testing

### Manual Testing with Swagger

1. Run the application
2. Open `https://localhost:{port}/swagger`
3. Test endpoints interactively

### Using Postman

Import the following collection:

**Base URL:** `https://localhost:5001`

**Test Sequence:**
1. POST `/api/chat/session` - Create a session
2. POST `/api/chat/message` - Send a message
3. GET `/api/chat/session/{id}` - Retrieve session
4. GET `/api/chat/sessions` - List all sessions
5. GET `/api/chat/mcp/status` - Check MCP status
6. DELETE `/api/chat/session/{id}` - Delete session

### Using cURL

**Complete Test Flow:**

```bash
# 1. Create a session
SESSION_ID=$(curl -s -X POST "https://localhost:5001/api/chat/session" \
  -H "Content-Type: application/json" \
  -d '{"title":"Test Session"}' \
  | jq -r '.data.id')

echo "Created session: $SESSION_ID"

# 2. Send a message
curl -X POST "https://localhost:5001/api/chat/message" \
  -H "Content-Type: application/json" \
  -d "{\"message\":\"Hello!\",\"sessionId\":\"$SESSION_ID\"}"

# 3. Get session details
curl "https://localhost:5001/api/chat/session/$SESSION_ID"

# 4. List all sessions
curl "https://localhost:5001/api/chat/sessions"

# 5. Check MCP status
curl "https://localhost:5001/api/chat/mcp/status"

# 6. Delete the session
curl -X DELETE "https://localhost:5001/api/chat/session/$SESSION_ID"
```

---

## 🚢 Deployment

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["LLM.Chat.API/LLM.Chat.API.csproj", "LLM.Chat.API/"]
RUN dotnet restore "LLM.Chat.API/LLM.Chat.API.csproj"
COPY . .
WORKDIR "/src/LLM.Chat.API"
RUN dotnet build "LLM.Chat.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LLM.Chat.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LLM.Chat.API.dll"]
```

Build and run:
```bash
docker build -t llm-chat-api .
docker run -p 8080:80 llm-chat-api
```

### Azure App Service

```bash
az webapp up --name llm-chat-api --resource-group myResourceGroup
```

### Production Checklist

- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Configure proper CORS policies
- [ ] Use HTTPS only
- [ ] Store API keys in Azure Key Vault or similar
- [ ] Configure log retention policies
- [ ] Set up database backups
- [ ] Enable application insights
- [ ] Configure rate limiting
- [ ] Set up health checks
- [ ] Review security headers

---

## 📦 Dependencies

### NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| LiteDB | 5.0.21 | Embedded NoSQL database |
| NLog.Web.AspNetCore | 6.0.5 | Logging framework |
| Microsoft.Extensions.AI.AzureAIInference | 9.4.0-preview.1 | Azure AI integration |
| Microsoft.Extensions.AI.Ollama | 9.4.0-preview.1 | Ollama integration |
| Microsoft.Extensions.AI.OpenAI | 9.4.0-preview.1 | OpenAI integration |
| ModelContextProtocol | 0.1.0-preview.10 | MCP client |
| Azure.AI.OpenAI | 2.2.0-beta.4 | Azure OpenAI SDK |
| Azure.Identity | 1.14.0-beta.3 | Azure authentication |
| OpenAI | 2.5.0 | OpenAI SDK |
| Swashbuckle.AspNetCore | 6.6.2 | Swagger/OpenAPI |

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Open a Pull Request

---

## 📄 License

This project is part of the Aspire.MCP.Sample solution.

---

## 📞 Support

For issues and questions:
- Check the logs in `logs/` directory
- Review the Swagger documentation at `/swagger`
- Check MCP server status via `/api/chat/mcp/status`

---

## 🔗 Related Projects

- [McpSample.BlazorChat](../src/McpSample.Chat/) - Blazor UI for chat
- [McpSample.PostgreSQLMCPServer](../src/McpSample.PostgreSQLMCPServer/) - MCP server
- [McpSample.AppHost](../src/McpSample.AppHost/) - Aspire orchestration

---

**Last Updated:** 2025-11-05
**Version:** 1.0.0
