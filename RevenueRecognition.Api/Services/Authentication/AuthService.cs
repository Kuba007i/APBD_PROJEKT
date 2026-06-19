using Microsoft.EntityFrameworkCore;
using RevenueRecognition.Api.Common.Authentication;
using RevenueRecognition.Api.Common.Exceptions;
using RevenueRecognition.Api.Contracts.Authentication;
using RevenueRecognition.Api.Data;

namespace RevenueRecognition.Api.Services.Authentication;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        AppDbContext dbContext,
        IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request)
    {
        var normalizedLogin =
            request.Login.Trim();

        var employee = await _dbContext.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(employee =>
                employee.Login == normalizedLogin);

        if (employee is null)
        {
            throw new UnauthorizedException(
                "Nieprawidłowy login lub hasło.");
        }

        var passwordIsValid =
            BCrypt.Net.BCrypt.Verify(
                request.Password,
                employee.PasswordHash);

        if (!passwordIsValid)
        {
            throw new UnauthorizedException(
                "Nieprawidłowy login lub hasło.");
        }

        var token =
            _jwtTokenService.CreateToken(employee);

        return new LoginResponse
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            Login = employee.Login,
            Role = employee.Role.ToString()
        };
    }
}