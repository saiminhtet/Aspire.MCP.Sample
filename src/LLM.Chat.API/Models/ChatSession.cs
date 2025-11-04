namespace LLM.Chat.API.Models;

public class ChatSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "New Chat";
    public List<ChatMessage> Messages { get; set; } = new();
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");
}
