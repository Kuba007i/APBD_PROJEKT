namespace RevenueRecognition.Api.Domain.Entities;

public sealed class ContractPayment
{
    public int Id { get; set; }

    public int ClientId { get; set; }

    public Client Client { get; set; } = null!;

    public int ContractId { get; set; }

    public Contract Contract { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateTime PaidAt { get; set; }

    public bool IsRefunded { get; set; }

    public DateTime? RefundedAt { get; set; }
}