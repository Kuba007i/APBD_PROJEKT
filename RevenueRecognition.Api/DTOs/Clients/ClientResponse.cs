namespace RevenueRecognition.Api.Contracts.Clients;

public sealed class ClientResponse
{
    public int Id { get; init; }

    public string ClientType { get; init; } = null!;

    public string Address { get; init; } = null!;

    public string Email { get; init; } = null!;

    public string PhoneNumber { get; init; } = null!;

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Pesel { get; init; }

    public string? CompanyName { get; init; }

    public string? Krs { get; init; }
}