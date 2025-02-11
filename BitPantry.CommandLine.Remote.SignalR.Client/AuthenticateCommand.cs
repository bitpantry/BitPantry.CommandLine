using BitPantry.CommandLine.API;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    [Command(Namespace = "server", Name = "auth")]
    [Description("Authenticates the client to retrieve a JWT token used in subsequent calls")]
    public class AuthenticateCommand : CommandBase
    {
        [Argument]
        [Alias('u')]
        [Description("The remote uri to use for authentication")]
        public string Uri { get; set; }

        public async Task Execute(CommandExecutionContext ctx)
        {
            Console.WriteLine();

            // get username

            var username = Console.Prompt(new TextPrompt<string>("Username: ")
                .Validate(un =>
                {
                    if (string.IsNullOrEmpty(un))
                    {
                        ValidationResult.Error("Username is required");
                        return false;
                    }

                    ValidationResult.Success();
                    return true;
                }));

            // get password

            var password = Console.Prompt(new TextPrompt<string>("Password: ")
                .Validate(un =>
                {
                    if (string.IsNullOrEmpty(un))
                    {
                        ValidationResult.Error("Password is required");
                        return false;
                    }

                    ValidationResult.Success();
                    return true;
                })
                .Secret());

            // create request

            var credentials = new { Username = username, Password = password };
            var jsonContent = new StringContent(JsonSerializer.Serialize(credentials), Encoding.UTF8, "application/json");

            // post

            var response = await new HttpClient().PostAsync(Uri, jsonContent);

            // evaluate response

            if (!response.IsSuccessStatusCode)
            {
                Console.MarkupLineInterpolated($"[yellow]Authentication Failed[/] with status [yellow]{response.StatusCode}[/]");
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                Console.WriteLine();
                Console.MarkupLine("[green]Authentication Successful[/]");

                AuthenticationSettings.CurrentAccessToken = new AccessToken(tokenResponse.Token);
            }

            Console.WriteLine();
        }

        private class TokenResponse
        {
            public string Token { get; set; }
        }

    }
}
