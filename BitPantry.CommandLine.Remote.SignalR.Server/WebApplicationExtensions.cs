using BitPantry.CommandLine.Remote.SignalR.Server.Auth;
using BitPantry.CommandLine.Remote.SignalR.Server.Auth.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    public static class WebApplicationExtensions
    {
        public static WebApplication ConfigureCommandLineHub(this WebApplication app)
        {
            app.UseRouting();
            
            var hub = app.MapHub<CommandLineHub>(ServerSettings.HubUrlPattern);

            if (AuthenticationSettings.IsUsingAuthentication)
            {
                hub.RequireAuthorization();
                
                // refresh route

                app.MapPost(AuthenticationSettings.AuthenticationRoute, async (JwtCredentialsModel credentials, IJwtCallerAuthenticationLogic authLogic, JwtTokenValidationParameters validationParams) =>
                {
                    if (await authLogic.AuthenticateCredentials(credentials))
                    {
                        var claims = new[]
                        {
                            new Claim(JwtRegisteredClaimNames.Sub, credentials.Username),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            new Claim(ClaimTypes.Role, "Cli")
                        };

                        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(validationParams.IssuerSigningKey));
                        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                        var token = new JwtSecurityToken(
                            issuer: validationParams.ValidIssuer,
                            audience: validationParams.ValidAudience,
                            claims: claims,
                            expires: DateTime.UtcNow.AddHours(1),
                            signingCredentials: creds
                        );

                        return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
                    }

                    return Results.Unauthorized();
                });

                // refresh route

                app.MapPost(AuthenticationSettings.RefreshTokenRoute, async (
                    RefreshTokenRequest refreshRequest,
                    IJwtCallerAuthenticationLogic authLogic,
                    JwtTokenValidationParameters validationParams,
                    IRefreshTokenService refreshTokenService) =>
                {
                    // 1️⃣ Validate the refresh token
                    var storedRefreshToken = await refreshTokenService.GetStoredRefreshToken(refreshRequest.RefreshToken);
                    if (storedRefreshToken == null || storedRefreshToken.Expires < DateTime.UtcNow)
                    {
                        return Results.Unauthorized(); // Invalid or expired refresh token
                    }

                    // 2️⃣ Validate the user associated with the refresh token
                    var user = await authLogic.AuthenticateCredentials(storedRefreshToken.UserId);
                    if (user == null)
                    {
                        return Results.Unauthorized(); // User no longer exists
                    }

                    // 3️⃣ Generate new JWT access token
                    var claims = new[]
                    {
                        new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "Cli"),
                    };

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(validationParams.IssuerSigningKey));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                    var newAccessToken = new JwtSecurityToken(
                        issuer: validationParams.ValidIssuer,
                        audience: validationParams.ValidAudience,
                        claims: claims,
                        expires: DateTime.UtcNow.AddHours(1), // New access token expiration
                        signingCredentials: creds
                    );

                    // 4️⃣ (Optional) Rotate Refresh Token (Generate a new one)
                    var newRefreshToken = refreshTokenService.GenerateRefreshToken(user.Id);
                    await refreshTokenService.StoreRefreshToken(newRefreshToken);

                    // 5️⃣ Return new access + refresh tokens
                    return Results.Ok(new
                    {
                        accessToken = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
                        refreshToken = newRefreshToken.Token // Send the new refresh token
                    });
                });

            }

            return app;
        }

        public static WebApplication UseCommandLineHubAuthorizationFilter(this WebApplication app)
        {

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments(ServerSettings.HubUrlPattern) 
                    && !context.Request.Headers.ContainsKey("Authorization") 
                    && context.Request.Query.TryGetValue("access_token", out var token))
                        context.Request.Headers.Append("Authorization", $"Bearer {token}");

                await next(); 
            });

            return app;
        }

    }

    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }

}
