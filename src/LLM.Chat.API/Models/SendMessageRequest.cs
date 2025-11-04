namespace LLM.Chat.API.Models;

public class SendMessageRequest
{
    public string Message { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public object? Context { get; set; }
}

public class SendMessageResponse
{
    public ChatMessage Message { get; set; } = new();
    public string SessionId { get; set; } = string.Empty;
    public List<string> Suggestions { get; set; } = new();
}

public class CreateSessionRequest
{
    public string? Title { get; set; }
}
