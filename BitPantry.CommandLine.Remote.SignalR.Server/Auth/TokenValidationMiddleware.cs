using BitPantry.CommandLine.Remote.SignalR.Server;
using BitPantry.CommandLine.Remote.SignalR.Server.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;

public class TokenValidationMiddleware
{
    private readonly ILogger<TokenValidationMiddleware> _logger;
    private readonly RequestDelegate _next;
    private readonly ITokenService _tokenSvc;
    private readonly ServerSettings _svrSettings;
    private readonly TokenAuthenticationSettings _tokenAuthSettings;

    public TokenValidationMiddleware(ILogger<TokenValidationMiddleware> logger, RequestDelegate next, ITokenService tokenSvc, ServerSettings svrSettings, TokenAuthenticationSettings tokenAuthSettings)
    {
        _logger = logger;
        _next = next;
        _tokenSvc = tokenSvc;
        _svrSettings = svrSettings;
        _tokenAuthSettings = tokenAuthSettings;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments(_svrSettings.HubUrlPattern))
        {
            var token = context.Request.Query["access_token"];

            var validationResult = new Tuple<bool, ClaimsPrincipal>(false, null);

            try { validationResult = await _tokenSvc.ValidateToken(token); }
            catch (Exception ex) { _logger.LogError(ex, "An error occured while validating the access token"); }

            if (!validationResult.Item1)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var errorResponse = new
                {
                    error = "Unauthorized",
                    message = "Token is missing, invalid, or expired. Use an API key to request a new token.",
                    token_request_endpoint = _tokenAuthSettings.TokenRequestEndpoint,
                    token_format = _tokenAuthSettings.TokenFormat
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                return;
            }

            context.Items["User"] = validationResult.Item2;

        }

        await _next(context);
    }
}

