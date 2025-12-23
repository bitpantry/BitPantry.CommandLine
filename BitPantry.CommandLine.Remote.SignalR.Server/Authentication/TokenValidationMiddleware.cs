using BitPantry.CommandLine.Remote.SignalR.Server.Authentication;
using BitPantry.CommandLine.Remote.SignalR.Server.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;
using BitPantry.CommandLine.Remote.SignalR.Server;

/// <summary>
/// Authorizes <see cref="CommandLineHub"/> traffic 
/// </summary>
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
            // pulls the access token from the request
            // First check Authorization header (preferred), then fall back to query string for SignalR negotiation
            string token = null;
            
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader.Substring("Bearer ".Length).Trim();
            }
            else
            {
                // Fall back to query string for SignalR WebSocket connections
                token = context.Request.Query["access_token"];
            }

            // validates the token

            var validationResult = new Tuple<bool, ClaimsPrincipal>(false, null);
           
            try { validationResult = await _tokenSvc.ValidateToken(token); }
            catch (Exception ex) { _logger.LogError(ex, "An error occured while validating the access token"); }

            if (!validationResult.Item1) // return an unauthorized response with authentication information if token could not be validated
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

            // if token is validated, sets the ClaimsPrincipal to the context for the request

            context.Items["User"] = validationResult.Item2;

        }

        await _next(context);
    }
}

