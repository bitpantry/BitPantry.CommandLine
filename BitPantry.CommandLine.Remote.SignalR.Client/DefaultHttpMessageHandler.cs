/// <summary>
/// Extends the <see cref="HttpClientHandler"/> to throw an exception when receiving an unauthorized
/// response. This ensures SignalR connection attempts fail immediately on 401.
/// </summary>
public class DefaultHttpMessageHandler : HttpClientHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if(response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException("Unauthorized", null, System.Net.HttpStatusCode.Unauthorized);
        }

        return response;
    }
}
