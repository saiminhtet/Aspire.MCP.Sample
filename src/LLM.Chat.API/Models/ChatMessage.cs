namespace LLM.Chat.API.Models;

public class ChatMessage
{
    public string Id { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "user" or "assistant" or "tool"
    public string Content { get; set; } = string.Empty;
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    public MessageMetadata? Metadata { get; set; }
}

public class MessageMetadata
{
    public string Model { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public List<string> McpToolsUsed { get; set; } = new();
}
