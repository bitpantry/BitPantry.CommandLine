using BitPantry.CommandLine.Remote.SignalR.Server.Auth;
using BitPantry.CommandLine.Remote.SignalR.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests.Auth;

[TestClass]
public class TokenValidationMiddlewareTests
{
    private Mock<ITokenService> _mockTokenService;
    private ServerSettings _serverSettings;
    private TokenAuthenticationSettings _tokenAuthSettings;
    private Mock<RequestDelegate> _mockNext;
    private Mock<ILogger<TokenValidationMiddleware>> _mockLogger;
    private TokenValidationMiddleware _middleware;

    [TestInitialize]
    public void Setup()
    {
        _mockTokenService = new Mock<ITokenService>();
        _serverSettings = new ServerSettings("/hub");
        _tokenAuthSettings = new TokenAuthenticationSettings(
            "somereallylongstringthatmeetsthe128byterequirement",
            "/token",
            "/refresh",
            TimeSpan.FromHours(1),
            TimeSpan.FromDays(30),
            TimeSpan.Zero,
            "issuer",
            "audience");
        _mockNext = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<TokenValidationMiddleware>>();

        _middleware = new TokenValidationMiddleware(
            _mockLogger.Object,
            _mockNext.Object,
            _mockTokenService.Object,
            _serverSettings,
            _tokenAuthSettings
        );
    }

    [TestMethod]
    public async Task Invoke_TokenMissing_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/hub";

        _mockTokenService.Setup(s => s.ValidateToken(It.Is<string>(token => string.IsNullOrEmpty(token))))
            .ReturnsAsync(new Tuple<bool, ClaimsPrincipal>(false, null));

        await _middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task Invoke_TokenInvalid_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/hub";
        context.Request.QueryString = new QueryString("?access_token=invalid_token");

        _mockTokenService.Setup(s => s.ValidateToken("invalid_token"))
            .ReturnsAsync(new Tuple<bool, ClaimsPrincipal>(false, null));

        await _middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task Invoke_TokenExpired_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/hub";
        context.Request.QueryString = new QueryString("?access_token=expired_token");

        _mockTokenService.Setup(s => s.ValidateToken("expired_token"))
            .ReturnsAsync(new Tuple<bool, ClaimsPrincipal>(false, null));

        await _middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task Invoke_TokenValid_AllowsRequest()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/hub";
        context.Request.QueryString = new QueryString("?access_token=valid_token");

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, "test_user")
        }));

        _mockTokenService.Setup(s => s.ValidateToken("valid_token"))
            .ReturnsAsync(new Tuple<bool, ClaimsPrincipal>(true, claimsPrincipal));

        await _middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context.Items["User"].Should().Be(claimsPrincipal);
    }

    [TestMethod]
    public async Task Invoke_TokenWithInvalidSignature_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/hub";
        context.Request.QueryString = new QueryString("?access_token=invalid_signature_token");

        _mockTokenService.Setup(s => s.ValidateToken("invalid_signature_token"))
            .ReturnsAsync(new Tuple<bool, ClaimsPrincipal>(false, null));

        await _middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task Invoke_TokenWithIncorrectAudienceOrIssuer_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/hub";
        context.Request.QueryString = new QueryString("?access_token=incorrect_audience_issuer_token");

        _mockTokenService.Setup(s => s.ValidateToken("incorrect_audience_issuer_token"))
            .ReturnsAsync(new Tuple<bool, ClaimsPrincipal>(false, null));

        await _middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task Invoke_TokenWithMissingClaims_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/hub";
        context.Request.QueryString = new QueryString("?access_token=missing_claims_token");

        _mockTokenService.Setup(s => s.ValidateToken("missing_claims_token"))
            .ReturnsAsync(new Tuple<bool, ClaimsPrincipal>(false, null));

        await _middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task Invoke_TokenWithRefreshTokenType_ValidatesAgainstRefreshTokenStore()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/hub";
        context.Request.QueryString = new QueryString("?access_token=refresh_token");

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test_client"),
            new Claim("token_type", "refresh")
        }));

        _mockTokenService.Setup(s => s.ValidateToken("refresh_token"))
            .ReturnsAsync(new Tuple<bool, ClaimsPrincipal>(true, claimsPrincipal));

        await _middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context.Items["User"].Should().Be(claimsPrincipal);
    }

    [TestMethod]
    public async Task Invoke_TokenValidationServiceFailure_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/hub";
        context.Request.QueryString = new QueryString("?access_token=token");

        _mockTokenService.Setup(s => s.ValidateToken("token"))
            .ReturnsAsync(new Tuple<bool, ClaimsPrincipal>(false, null));

        await _middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task Invoke_TokenValidationServiceException_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/hub";
        context.Request.QueryString = new QueryString("?access_token=token");

        _mockTokenService.Setup(s => s.ValidateToken("token"))
            .ThrowsAsync(new Exception("Validation error"));

        await _middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task Invoke_MalformedToken_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/hub";
        context.Request.QueryString = new QueryString("?access_token=malformed_token");

        _mockTokenService.Setup(s => s.ValidateToken("malformed_token"))
            .ReturnsAsync(new Tuple<bool, ClaimsPrincipal>(false, null));

        await _middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task Invoke_TokenWithInsufficientClaims_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/hub";
        context.Request.QueryString = new QueryString("?access_token=insufficient_claims_token");

        _mockTokenService.Setup(s => s.ValidateToken("insufficient_claims_token"))
            .ReturnsAsync(new Tuple<bool, ClaimsPrincipal>(false, null));

        await _middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task Invoke_TokenWithExpiredRefreshToken_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/hub";
        context.Request.QueryString = new QueryString("?access_token=expired_refresh_token");

        _mockTokenService.Setup(s => s.ValidateToken("expired_refresh_token"))
            .ReturnsAsync(new Tuple<bool, ClaimsPrincipal>(false, null));

        await _middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task Invoke_TokenWithValidSignatureButInvalidClaims_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/hub";
        context.Request.QueryString = new QueryString("?access_token=valid_signature_invalid_claims_token");

        _mockTokenService.Setup(s => s.ValidateToken("valid_signature_invalid_claims_token"))
            .ReturnsAsync(new Tuple<bool, ClaimsPrincipal>(false, null));

        await _middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task Invoke_TokenWithValidSignatureButIncorrectAudience_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/hub";
        context.Request.QueryString = new QueryString("?access_token=valid_signature_incorrect_audience_token");

        _mockTokenService.Setup(s => s.ValidateToken("valid_signature_incorrect_audience_token"))
            .ReturnsAsync(new Tuple<bool, ClaimsPrincipal>(false, null));

        await _middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task Invoke_TokenWithValidSignatureButIncorrectIssuer_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/hub";
        context.Request.QueryString = new QueryString("?access_token=valid_signature_incorrect_issuer_token");

        _mockTokenService.Setup(s => s.ValidateToken("valid_signature_incorrect_issuer_token"))
            .ReturnsAsync(new Tuple<bool, ClaimsPrincipal>(false, null));

        await _middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task Invoke_TokenWithValidSignatureButIncorrectKey_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/hub";
        context.Request.QueryString = new QueryString("?access_token=valid_signature_incorrect_key_token");

        _mockTokenService.Setup(s => s.ValidateToken("valid_signature_incorrect_key_token"))
            .ReturnsAsync(new Tuple<bool, ClaimsPrincipal>(false, null));

        await _middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }
}

