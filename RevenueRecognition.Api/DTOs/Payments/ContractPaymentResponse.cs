namespace RevenueRecognition.Api.Contracts.Payments;

public sealed class ContractPaymentResponse
{
    public int Id { get; init; }

    public int ContractId { get; init; }

    public int ClientId { get; init; }

    public decimal Amount { get; init; }

    public DateTime PaidAt { get; init; }

    public decimal TotalPaid { get; init; }

    public decimal RemainingAmount { get; init; }

    public string ContractStatus { get; init; } = null!;

    public DateTime? SignedAt { get; init; }

    public DateTime? UpdatesUntil { get; init; }
}