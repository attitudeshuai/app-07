namespace PointsMall.Dtos;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

public static class ApiResponse
{
    public static ApiResponse<object> Ok(string message = "操作成功")
    {
        return new ApiResponse<object> { Success = true, Message = message };
    }

    public static ApiResponse<T> Ok<T>(T data, string message = "操作成功")
    {
        return new ApiResponse<T> { Success = true, Message = message, Data = data };
    }

    public static ApiResponse<object> Error(string message = "操作失败")
    {
        return new ApiResponse<object> { Success = false, Message = message };
    }

    public static ApiResponse<T> Error<T>(string message = "操作失败")
    {
        return new ApiResponse<T> { Success = false, Message = message };
    }
}
