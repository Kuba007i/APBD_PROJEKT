using System.ComponentModel.DataAnnotations;

namespace RevenueRecognition.Api.Contracts.Contracts;

public sealed class CreateContractRequest
{
    [Range(1, int.MaxValue)]
    public int ClientId { get; set; }

    [Range(1, int.MaxValue)]
    public int SoftwareId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [Range(
        0,
        3,
        ErrorMessage =
            "Liczba dodatkowych lat wsparcia musi wynosić od 0 do 3.")]
    public int AdditionalSupportYears { get; set; }
}