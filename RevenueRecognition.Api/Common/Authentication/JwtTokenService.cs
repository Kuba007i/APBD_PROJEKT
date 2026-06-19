using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RevenueRecognition.Api.Domain.Entities;

namespace RevenueRecognition.Api.Common.Authentication;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(
        IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public JwtTokenResult CreateToken(Employee employee)
    {
        var now = DateTime.UtcNow;

        var expiresAt =
            now.AddMinutes(_settings.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                employee.Id.ToString()),

            new(
                ClaimTypes.NameIdentifier,
                employee.Id.ToString()),

            new(
                ClaimTypes.Name,
                employee.Login),

            new(
                ClaimTypes.Role,
                employee.Role.ToString()),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.Key));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        var accessToken =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return new JwtTokenResult(
            accessToken,
            expiresAt);
    }
}