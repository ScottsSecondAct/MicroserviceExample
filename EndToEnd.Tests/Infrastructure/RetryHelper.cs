namespace EndToEnd.Tests.Infrastructure;

public static class RetryHelper
{
    public static async Task WaitUntilAsync(
        Func<Task<bool>> condition,
        TimeSpan? timeout = null,
        TimeSpan? interval = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (DateTime.UtcNow < deadline)
        {
            if (await condition()) return;
            await Task.Delay(interval ?? TimeSpan.FromMilliseconds(300));
        }
        throw new TimeoutException("Condition not met within timeout.");
    }
}
