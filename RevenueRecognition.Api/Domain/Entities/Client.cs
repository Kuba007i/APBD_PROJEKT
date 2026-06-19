namespace RevenueRecognition.Api.Domain.Entities;

public abstract class Client
{
    public int Id { get; set; }

    public string Address { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public ICollection<Contract> Contracts { get; set; } =
        new List<Contract>();

    public ICollection<ContractPayment> ContractPayments { get; set; } =
        new List<ContractPayment>();
}