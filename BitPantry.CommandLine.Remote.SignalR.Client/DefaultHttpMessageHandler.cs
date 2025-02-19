﻿public class DefaultHttpMessageHandler : HttpClientHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if(response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var ex = new HttpRequestException("Unauthorized", null, System.Net.HttpStatusCode.Unauthorized);
            ex.Data["responseBody"] = await response.Content.ReadAsStringAsync();

            throw ex;
        }

        return response;
    }
}
