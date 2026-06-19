namespace RevenueRecognition.Api.Domain.Entities;

public sealed class Software
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string CurrentVersion { get; set; } = null!;

    public string Category { get; set; } = null!;

    public decimal YearlyLicensePrice { get; set; }

    public ICollection<Discount> Discounts { get; set; } =
        new List<Discount>();

    public ICollection<Contract> Contracts { get; set; } =
        new List<Contract>();
}