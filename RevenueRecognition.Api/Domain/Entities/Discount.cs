using RevenueRecognition.Api.Domain.Enums;

namespace RevenueRecognition.Api.Domain.Entities;

public sealed class Discount
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public decimal Percentage { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidTo { get; set; }

    public DiscountTarget Target { get; set; }

    public int SoftwareId { get; set; }

    public Software Software { get; set; } = null!;
}