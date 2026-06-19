using RevenueRecognition.Api.Domain.Entities;

namespace RevenueRecognition.Api.Common.Authentication;

public interface IJwtTokenService
{
    JwtTokenResult CreateToken(Employee employee);
}