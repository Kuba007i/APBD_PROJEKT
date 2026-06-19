namespace RevenueRecognition.Api.Contracts.Contracts;

public sealed class ContractResponse
{
    public int Id { get; init; }

    public int ClientId { get; init; }

    public int SoftwareId { get; init; }

    public string SoftwareName { get; init; } = null!;

    public string SoftwareVersion { get; init; } = null!;

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public decimal BasePrice { get; init; }

    public int IncludedUpdateYears { get; init; }

    public int AdditionalSupportYears { get; init; }

    public decimal SupportCost { get; init; }

    public decimal ProductDiscountPercentage { get; init; }

    public decimal ReturningCustomerDiscountPercentage { get; init; }

    public decimal FinalPrice { get; init; }

    public string Status { get; init; } = null!;

    public DateTime? SignedAt { get; init; }
    
    public DateTime? UpdatesUntil { get; init; }
}