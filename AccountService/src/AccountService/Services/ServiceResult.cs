namespace AccountService.Services;

public class ServiceResult
{
  public bool IsSuccess { get; private set; }
  public string Message { get; private set; }
  public int StatusCode { get; private set; }
  public object? Data { get; private set; }

  private ServiceResult(bool isSuccess, string message, int statusCode, object? data = null)
  {
    IsSuccess = isSuccess;
    Message = message;
    StatusCode = statusCode;
    Data = data;
  }

  public static ServiceResult Success(object? data = null, string message = "Success", int statusCode = 200) =>
    new(true, message, statusCode, data);

  public static ServiceResult Failure(string message = "Bad Request", int statusCode = 400) =>
    new(false, message, statusCode);

  public static ServiceResult Error(string message = "Internal Server Error", int statusCode = 500) =>
    new(false, message, statusCode);
}
