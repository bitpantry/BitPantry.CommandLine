
namespace BitPantry.CommandLine.Remote.SignalR.Server.Auth.Jwt
{
    public interface IJwtCallerAuthenticationLogic
    {
        Task<bool> AuthenticateCredentials(JwtCredentialsModel request);
    }
}