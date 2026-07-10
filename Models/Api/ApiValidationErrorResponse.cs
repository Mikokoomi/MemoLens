namespace MemoLens.Models.Api;

public class ApiValidationErrorResponse : ApiResponse
{
    public IDictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();
}
