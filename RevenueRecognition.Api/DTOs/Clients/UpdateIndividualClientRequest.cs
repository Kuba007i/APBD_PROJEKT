using System.ComponentModel.DataAnnotations;

namespace RevenueRecognition.Api.Contracts.Clients;

public sealed class UpdateIndividualClientRequest
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
}