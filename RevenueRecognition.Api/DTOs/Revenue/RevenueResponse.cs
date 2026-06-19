namespace RevenueRecognition.Api.Contracts.Revenue;

public sealed class RevenueResponse
{
    public string RevenueType { get; init; } = null!;

    public int? SoftwareId { get; init; }

    public string? SoftwareName { get; init; }

    public decimal AmountInPln { get; init; }

    public string Currency { get; init; } = null!;

    public decimal ExchangeRate { get; init; }

    public decimal ConvertedAmount { get; init; }

    public DateTime CalculatedAt { get; init; }
}