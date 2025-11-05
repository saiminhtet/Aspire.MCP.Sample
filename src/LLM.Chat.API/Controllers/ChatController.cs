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

            // Extract assistant message
            var assistantResponse = response.Messages.FirstOrDefault(m => m.Role == ChatRole.Assistant);
            string aiContent = assistantResponse?.Text ?? "I'm sorry, I couldn't process that request.";

            // Create assistant message
            var aiMessage = new Models.ChatMessage
            {
                Id = $"msg-{DateTime.UtcNow.Ticks}-assistant",
                Role = "assistant",
                Content = aiContent,
                Timestamp = DateTime.UtcNow.ToString("o"),
                Metadata = new MessageMetadata
                {
                    Model = _chatClient.GetType().Name,
                    TokensUsed = 0, // You can calculate this if available
                    McpToolsUsed = new List<string>()
                }
            };
            session.Messages.Add(aiMessage);

            // Save session
            _dbService.UpdateSession(session);

            var suggestions = new List<string>
            {
                "Tell me more",
                "Can you explain that differently?",
                "What else can you help with?"
            };

            var responseData = new SendMessageResponse
            {
                Message = aiMessage,
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
