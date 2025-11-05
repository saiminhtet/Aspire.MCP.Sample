namespace LLM.Chat.API.Models;

public class ApiResponse<T>
{
    public bool Succeeded { get; set; }
    public bool Failed { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }

    public static ApiResponse<T> Success(T data)
    {
        return new ApiResponse<T>
        {
            Succeeded = true,
            Failed = false,
            Data = data
        };
    }

    public static ApiResponse<T> Failure(string message)
    {
        return new ApiResponse<T>
        {
            Succeeded = false,
            Failed = true,
            Message = message
        };
    }
}
