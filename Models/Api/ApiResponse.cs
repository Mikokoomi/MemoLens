namespace MemoLens.Models.Api;

public class ApiResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; init; }
}
