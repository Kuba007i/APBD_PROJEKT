namespace RevenueRecognition.Api.Contracts.Authentication;

public sealed class LoginResponse
{
    public string AccessToken { get; init; } = null!;

    public string TokenType { get; init; } = "Bearer";

    public DateTime ExpiresAt { get; init; }

    public string Login { get; init; } = null!;

    public string Role { get; init; } = null!;
}