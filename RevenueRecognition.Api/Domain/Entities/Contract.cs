using RevenueRecognition.Api.Domain.Enums;

namespace RevenueRecognition.Api.Domain.Entities;

public sealed class Contract
{
    public int Id { get; set; }

    public int ClientId { get; set; }

    public Client Client { get; set; } = null!;

    public int SoftwareId { get; set; }

    public Software Software { get; set; } = null!;
    
    public string SoftwareVersion { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal BasePrice { get; set; }

    public int AdditionalSupportYears { get; set; }

    public decimal SupportCost { get; set; }

    public decimal ProductDiscountPercentage { get; set; }

    public decimal ReturningCustomerDiscountPercentage { get; set; }

    public decimal FinalPrice { get; set; }

    public ContractStatus Status { get; set; } =
        ContractStatus.Pending;

    public DateTime? SignedAt { get; set; }
    
    public DateTime? UpdatesUntil { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<ContractPayment> Payments { get; set; } =
        new List<ContractPayment>();
}