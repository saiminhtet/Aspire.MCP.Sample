using LiteDB;
using LLM.Chat.API.Models;

namespace LLM.Chat.API.Services;

public interface IChatDatabaseService
{
    ChatSession? GetSession(string sessionId);
    List<ChatSession> GetAllSessions();
    ChatSession CreateSession(string? title = null);
    ChatSession UpdateSession(ChatSession session);
    bool DeleteSession(string sessionId);
    ChatSession AddMessageToSession(string sessionId, ChatMessage message);
}

public class ChatDatabaseService : IChatDatabaseService, IDisposable
{
    private readonly LiteDatabase _database;
    private readonly ILiteCollection<ChatSession> _sessions;

    public ChatDatabaseService(string databasePath = "chat.db")
    {
        _database = new LiteDatabase(databasePath);
        _sessions = _database.GetCollection<ChatSession>("sessions");
        _sessions.EnsureIndex(x => x.Id);
    }

    public ChatSession? GetSession(string sessionId)
    {
        return _sessions.FindById(sessionId);
    }

    public List<ChatSession> GetAllSessions()
    {
        return _sessions.FindAll()
            .OrderByDescending(s => s.UpdatedAt)
            .ToList();
    }

    public ChatSession CreateSession(string? title = null)
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid().ToString(),
            Title = title ?? "New Chat",
            Messages = new List<ChatMessage>(),
            CreatedAt = DateTime.UtcNow.ToString("o"),
            UpdatedAt = DateTime.UtcNow.ToString("o")
        };

        _sessions.Insert(session);
        return session;
    }

    public ChatSession UpdateSession(ChatSession session)
    {
        session.UpdatedAt = DateTime.UtcNow.ToString("o");
        _sessions.Update(session);
        return session;
    }

    public bool DeleteSession(string sessionId)
    {
        return _sessions.Delete(sessionId);
    }

    public ChatSession AddMessageToSession(string sessionId, ChatMessage message)
    {
        var session = GetSession(sessionId);
        if (session == null)
        {
            throw new ArgumentException($"Session with ID {sessionId} not found");
        }

        session.Messages.Add(message);
        return UpdateSession(session);
    }

    public void Dispose()
    {
        _database?.Dispose();
    }
}
