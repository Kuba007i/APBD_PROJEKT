namespace RevenueRecognition.Api.Common.Authentication;

public sealed record JwtTokenResult(
    string AccessToken,
    DateTime ExpiresAt);