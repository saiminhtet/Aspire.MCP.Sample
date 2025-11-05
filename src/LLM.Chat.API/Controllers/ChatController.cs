using LLM.Chat.API.Models;
using LLM.Chat.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using AIChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace LLM.Chat.API.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IChatDatabaseService _dbService;
    private readonly IChatClient _chatClient;
    private readonly IMcpClient _mcpClient;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatDatabaseService dbService,
        IChatClient chatClient,
        IMcpClient mcpClient,
        ILogger<ChatController> logger)
    {
        _dbService = dbService;
        _chatClient = chatClient;
        _mcpClient = mcpClient;
        _logger = logger;
    }

    [HttpPost("message")]
    public async Task<ActionResult<ApiResponse<SendMessageResponse>>> SendMessage([FromBody] SendMessageRequest request)
    {
        try
        {
            _logger.LogInformation($"Received message: {request.Message}");

            // Get or create session
            ChatSession? session;
            if (string.IsNullOrEmpty(request.SessionId))
            {
                session = _dbService.CreateSession(request.Message.Length > 50
                    ? request.Message.Substring(0, 50) + "..."
                    : request.Message);
                _logger.LogInformation($"Created new session: {session.Id}");
            }
            else
            {
                session = _dbService.GetSession(request.SessionId);
                if (session == null)
                {
                    return NotFound(ApiResponse<SendMessageResponse>.Failure("Session not found"));
                }
            }

            // Add user message to session
            var userMessage = new Models.ChatMessage
            {
                Id = $"msg-{DateTime.UtcNow.Ticks}-user",
                Role = "user",
                Content = request.Message,
                Timestamp = DateTime.UtcNow.ToString("o")
            };
            session.Messages.Add(userMessage);

            // Prepare messages for AI
            var aiMessages = new List<AIChatMessage>
            {
                new(ChatRole.System, @"You are a helpful AI assistant integrated with a Chat UI that uses the OpenAI Model Context Protocol (MCP).
                    You can call tools and functions. Be friendly and approachable. Keep responses concise and easy to read.")
            };

            // Add conversation history
            foreach (var msg in session.Messages)
            {
                var role = msg.Role switch
                {
                    "user" => ChatRole.User,
                    "assistant" => ChatRole.Assistant,
                    "tool" => ChatRole.Tool,
                    _ => ChatRole.User
                };
                aiMessages.Add(new AIChatMessage(role, msg.Content));
            }

            // Get MCP tools
            var tools = await _mcpClient.ListToolsAsync();
            _logger.LogInformation($"Available MCP tools: {tools.Count}");

            // Get AI response
            var response = await _chatClient.GetResponseAsync(aiMessages, new() { Tools = [.. tools] });
            _logger.LogInformation($"AI response received with {response.Messages.Count} messages");

            // Log all messages in the response for debugging
            foreach (var msg in response.Messages)
            {
                _logger.LogInformation($"Response Message - Role: {msg.Role}, HasText: {!string.IsNullOrEmpty(msg.Text)}, ContentCount: {msg.Contents?.Count ?? 0}");
            }

            // Extract the LAST assistant message (after tool execution)
            // When UseFunctionInvocation is used, response.Messages contains:
            // 1. Assistant message with tool call (no text)
            // 2. Tool message with result
            // 3. Final assistant message with response text
            var assistantResponse = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant);
            string aiContent = assistantResponse?.Text ?? "I'm sorry, I couldn't process that request.";

            _logger.LogInformation($"Final assistant response: {aiContent}");

            // Track which MCP tools were used
            var toolsUsed = response.Messages
                .Where(m => m.Role == ChatRole.Tool)
                .SelectMany(m => m.Contents ?? new List<AIContent>())
                .OfType<FunctionResultContent>()
                .Select(f => f.CallId ?? "unknown")
                .ToList();

            // Add all response messages to session (tool calls, tool results, and final response)
            foreach (var msg in response.Messages)
            {
                var msgRole = msg.Role.Value switch
                {
                    "assistant" => "assistant",
                    "tool" => "tool",
                    _ => "assistant"
                };

                var msgContent = msg.Text ?? string.Empty;

                // For tool messages, extract the result
                if (msg.Role == ChatRole.Tool && msg.Contents?.Any() == true)
                {
                    var toolResult = msg.Contents.OfType<FunctionResultContent>().FirstOrDefault();
                    if (toolResult != null)
                    {
                        msgContent = $"Tool: {toolResult.CallId}\nResult: {toolResult.Result}";
                    }
                }

                session.Messages.Add(new Models.ChatMessage
                {
                    Id = $"msg-{DateTime.UtcNow.Ticks}-{msgRole}-{Guid.NewGuid()}",
                    Role = msgRole,
                    Content = msgContent,
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    Metadata = new MessageMetadata
                    {
                        Model = _chatClient.GetType().Name,
                        TokensUsed = 0,
                        McpToolsUsed = msgRole == "assistant" ? toolsUsed : new List<string>()
                    }
                });
            }

            // Save session
            _dbService.UpdateSession(session);

            var suggestions = new List<string>
            {
                "Tell me more",
                "Can you explain that differently?",
                "What else can you help with?"
            };

            // Get the last assistant message to return to the client
            var finalMessage = session.Messages.LastOrDefault(m => m.Role == "assistant")
                ?? new Models.ChatMessage
                {
                    Id = $"msg-{DateTime.UtcNow.Ticks}-assistant",
                    Role = "assistant",
                    Content = "I'm sorry, I couldn't process that request.",
                    Timestamp = DateTime.UtcNow.ToString("o")
                };

            var responseData = new SendMessageResponse
            {
                Message = finalMessage,
                SessionId = session.Id,
                Suggestions = suggestions
            };

            return Ok(ApiResponse<SendMessageResponse>.Success(responseData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            return StatusCode(500, ApiResponse<SendMessageResponse>.Failure(ex.Message));
        }
    }

    [HttpGet("session/{id}")]
    public ActionResult<ApiResponse<ChatSession>> GetSession(string id)
    {
        try
        {
            var session = _dbService.GetSession(id);
            if (session == null)
            {
                return NotFound(ApiResponse<ChatSession>.Failure("Session not found"));
            }

            return Ok(ApiResponse<ChatSession>.Success(session));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving session {id}");
            return StatusCode(500, ApiResponse<ChatSession>.Failure(ex.Message));
        }
    }

    [HttpGet("sessions")]
    public ActionResult<ApiResponse<List<ChatSession>>> GetSessions()
    {
        try
        {
            var sessions = _dbService.GetAllSessions();
            return Ok(ApiResponse<List<ChatSession>>.Success(sessions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sessions");
            return StatusCode(500, ApiResponse<List<ChatSession>>.Failure(ex.Message));
        }
    }

    [HttpPost("session")]
    public ActionResult<ApiResponse<ChatSession>> CreateSession([FromBody] CreateSessionRequest request)
    {
        try
        {
            var session = _dbService.CreateSession(request.Title ?? "New Chat");
            return Ok(ApiResponse<ChatSession>.Success(session));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session");
            return StatusCode(500, ApiResponse<ChatSession>.Failure(ex.Message));
        }
    }

    [HttpDelete("session/{id}")]
    public ActionResult<ApiResponse<bool>> DeleteSession(string id)
    {
        try
        {
            var result = _dbService.DeleteSession(id);
            if (!result)
            {
                return NotFound(ApiResponse<bool>.Failure("Session not found"));
            }

            return Ok(ApiResponse<bool>.Success(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting session {id}");
            return StatusCode(500, ApiResponse<bool>.Failure(ex.Message));
        }
    }

    [HttpGet("mcp/status")]
    public async Task<ActionResult<ApiResponse<List<McpServerStatus>>>> GetMcpStatus()
    {
        try
        {
            var statusList = new List<McpServerStatus>();

            try
            {
                var tools = await _mcpClient.ListToolsAsync();
                statusList.Add(new McpServerStatus
                {
                    Name = "MCP Server",
                    Connected = true,
                    Error = null
                });
            }
            catch (Exception ex)
            {
                statusList.Add(new McpServerStatus
                {
                    Name = "MCP Server",
                    Connected = false,
                    Error = ex.Message
                });
            }

            return Ok(ApiResponse<List<McpServerStatus>>.Success(statusList));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting MCP status");
            return StatusCode(500, ApiResponse<List<McpServerStatus>>.Failure(ex.Message));
        }
    }
}
