using RevenueRecognition.Api.Contracts.Authentication;

namespace RevenueRecognition.Api.Services.Authentication;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(
        LoginRequest request);
}