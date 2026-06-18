namespace Middleground.Api.Models;

/// <summary>Consistent envelope for all API responses.</summary>
public sealed record ApiResponse<T>(bool Success, T? Data = default, string? Error = null)
{
    public static ApiResponse<T> Ok(T data) => new(true, data);
    public static ApiResponse<T> Fail(string error) => new(false, default, error);
}
