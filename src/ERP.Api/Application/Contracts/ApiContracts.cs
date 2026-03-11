namespace ERP.Api.Application.Contracts;

public sealed record ApiResponse<T>(bool Success, T? Data, ApiErrorResponse? Error);

public sealed record ApiErrorResponse(string Code, string Message);

public sealed record PaginationRequest(int Page = 1, int PageSize = 20);

public sealed record PagedResponse<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalItems);

public static class ApiResponses
{
    public static ApiResponse<T> Ok<T>(T data) => new(true, data, null);

    public static ApiResponse<object> Error(string code, string message) => new(false, null, new ApiErrorResponse(code, message));
}
