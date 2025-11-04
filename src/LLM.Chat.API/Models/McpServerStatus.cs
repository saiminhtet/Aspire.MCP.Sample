namespace LLM.Chat.API.Models;

public class McpServerStatus
{
    public string Name { get; set; } = string.Empty;
    public bool Connected { get; set; }
    public string? Error { get; set; }
}
