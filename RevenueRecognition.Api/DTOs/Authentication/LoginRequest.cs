using System.ComponentModel.DataAnnotations;

namespace RevenueRecognition.Api.Contracts.Authentication;

public sealed class LoginRequest
{
    [Required]
    [MaxLength(100)]
    public string Login { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Password { get; set; } = null!;
}