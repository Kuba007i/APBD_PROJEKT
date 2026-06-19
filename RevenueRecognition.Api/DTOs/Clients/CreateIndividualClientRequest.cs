using System.ComponentModel.DataAnnotations;

namespace RevenueRecognition.Api.Contracts.Clients;

public sealed class CreateIndividualClientRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Address { get; set; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = null!;

    [Required]
    [MaxLength(30)]
    public string PhoneNumber { get; set; } = null!;

    [Required]
    [RegularExpression(
        @"^\d{11}$",
        ErrorMessage = "PESEL musi składać się z dokładnie 11 cyfr.")]
    public string Pesel { get; set; } = null!;
}