using System.Text.Json.Serialization;

/// <summary>
/// Used to deserialize an unauthorized response body from the server
/// </summary>
public class UnauthorizedResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("token_request_endpoint")]
    public string TokenRequestEndpoint { get; set; }

    [JsonPropertyName("token_format")]
    public string TokenFormat { get; set; }
}
