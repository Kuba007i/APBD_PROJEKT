namespace RevenueRecognition.Api.Domain.Entities;

public sealed class CompanyClient : Client
{
    public string CompanyName { get; set; } = null!;

    public string Krs { get; set; } = null!;
}