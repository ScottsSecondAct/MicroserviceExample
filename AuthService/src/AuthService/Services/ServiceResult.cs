namespace AuthService.Services
{
  public class ServiceResult
  {
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; } // Equivalent to IActionStatus Code
    public object? Data { get; set; }   // Optional, for passing back additional info

    public static ServiceResult Success(object? data = null, string message = "Success", int statusCode = 200)
    {
      return new ServiceResult { IsSuccess = true, Message = message, StatusCode = statusCode, Data = data };
    }

    public static ServiceResult Failure(string message, int statusCode = 400)
    {
      return new ServiceResult { IsSuccess = false, Message = message, StatusCode = statusCode };
    }

    public static ServiceResult Error(string message, int statusCode = 500)
    {
      return new ServiceResult { IsSuccess = false, Message = message, StatusCode = statusCode };
    }
  }
}
