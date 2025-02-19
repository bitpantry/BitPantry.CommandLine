using Microsoft.AspNetCore.SignalR;

public class TestClientProxy : IClientProxy, ISingleClientProxy
{
    public List<(string Method, object[] Args, CancellationToken Token)> SentMessages { get; }
        = new List<(string, object[], CancellationToken)>();

    public Task<T> InvokeCoreAsync<T>(string method, object[] args, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<T>();

        // Store the method call for inspection
        SentMessages.Add((method, args, cancellationToken));

        // If T is a value type, return its default value
        tcs.SetResult(default(T));

        return tcs.Task;
    }

    public Task SendAsync(string method, object arg1, CancellationToken cancellationToken = default)
    {
        // Treat single argument as an array of one element for consistency
        return SendCoreAsync(method, new object[] { arg1 }, cancellationToken);
    }

    public Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken = default)
    {
        // Store the method call for inspection
        SentMessages.Add((method, args, cancellationToken));

        return Task.CompletedTask;
    }
}
