using System.ComponentModel.DataAnnotations;

namespace RevenueRecognition.Api.Contracts.Payments;

public sealed class CreateContractPaymentRequest
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Identyfikator klienta musi być większy od zera.")]
    public int ClientId { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "9999999999999999",
        ErrorMessage = "Kwota płatności musi być większa od zera.")]
    public decimal Amount { get; set; }
}