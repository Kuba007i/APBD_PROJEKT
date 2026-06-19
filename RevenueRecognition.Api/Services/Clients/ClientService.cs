using Microsoft.EntityFrameworkCore;
using RevenueRecognition.Api.Common.Exceptions;
using RevenueRecognition.Api.Contracts.Clients;
using RevenueRecognition.Api.Data;
using RevenueRecognition.Api.Domain.Entities;

namespace RevenueRecognition.Api.Services.Clients;

public sealed class ClientService : IClientService
{
    private readonly AppDbContext _dbContext;

    public ClientService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ClientResponse> CreateIndividualAsync(
        CreateIndividualClientRequest request)
    {
        var peselAlreadyExists = await _dbContext.IndividualClients
            .AnyAsync(client => client.Pesel == request.Pesel);

        if (peselAlreadyExists)
        {
            throw new ConflictException(
                $"Klient z numerem PESEL {request.Pesel} już istnieje.");
        }

        var client = new IndividualClient
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Address = request.Address.Trim(),
            Email = request.Email.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Pesel = request.Pesel
        };

        _dbContext.IndividualClients.Add(client);
        await _dbContext.SaveChangesAsync();

        return MapIndividual(client);
    }

    public async Task<ClientResponse> CreateCompanyAsync(
        CreateCompanyClientRequest request)
    {
        var krsAlreadyExists = await _dbContext.CompanyClients
            .AnyAsync(client => client.Krs == request.Krs);

        if (krsAlreadyExists)
        {
            throw new ConflictException(
                $"Firma z numerem KRS {request.Krs} już istnieje.");
        }

        var client = new CompanyClient
        {
            CompanyName = request.CompanyName.Trim(),
            Address = request.Address.Trim(),
            Email = request.Email.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Krs = request.Krs
        };

        _dbContext.CompanyClients.Add(client);
        await _dbContext.SaveChangesAsync();

        return MapCompany(client);
    }

    public async Task<ClientResponse> UpdateIndividualAsync(
        int clientId,
        UpdateIndividualClientRequest request)
    {
        var client = await _dbContext.IndividualClients
            .SingleOrDefaultAsync(client =>
                client.Id == clientId &&
                !client.IsDeleted);

        if (client is null)
        {
            throw new NotFoundException(
                $"Nie znaleziono klienta indywidualnego o ID {clientId}.");
        }

        client.FirstName = request.FirstName.Trim();
        client.LastName = request.LastName.Trim();
        client.Address = request.Address.Trim();
        client.Email = request.Email.Trim();
        client.PhoneNumber = request.PhoneNumber.Trim();

        await _dbContext.SaveChangesAsync();

        return MapIndividual(client);
    }

    public async Task<ClientResponse> UpdateCompanyAsync(
        int clientId,
        UpdateCompanyClientRequest request)
    {
        var client = await _dbContext.CompanyClients
            .SingleOrDefaultAsync(client => client.Id == clientId);

        if (client is null)
        {
            throw new NotFoundException(
                $"Nie znaleziono firmy o ID {clientId}.");
        }

        client.CompanyName = request.CompanyName.Trim();
        client.Address = request.Address.Trim();
        client.Email = request.Email.Trim();
        client.PhoneNumber = request.PhoneNumber.Trim();

        await _dbContext.SaveChangesAsync();

        return MapCompany(client);
    }

    public async Task DeleteAsync(int clientId)
    {
        var client = await _dbContext.Clients
            .SingleOrDefaultAsync(client =>
                client.Id == clientId &&
                !client.IsDeleted);

        if (client is null)
        {
            throw new NotFoundException(
                $"Nie znaleziono klienta o ID {clientId}.");
        }

        if (client is CompanyClient)
        {
            throw new BusinessRuleException(
                "Dane firmy nie mogą zostać usunięte.");
        }

        if (client is not IndividualClient individualClient)
        {
            throw new BusinessRuleException(
                "Nieobsługiwany typ klienta.");
        }

        AnonymizeIndividualClient(individualClient);

        await _dbContext.SaveChangesAsync();
    }

    private static void AnonymizeIndividualClient(
        IndividualClient client)
    {
        var anonymizedIdentifier =
            $"DELETED_{client.Id}_{Guid.NewGuid():N}"[..30];

        client.FirstName = "DELETED";
        client.LastName = "DELETED";
        client.Address = "DELETED";
        client.Email = $"{anonymizedIdentifier}@deleted.local";
        client.PhoneNumber = "DELETED";
        client.Pesel = anonymizedIdentifier;
        client.IsDeleted = true;
    }

    private static ClientResponse MapIndividual(
        IndividualClient client)
    {
        return new ClientResponse
        {
            Id = client.Id,
            ClientType = "Individual",
            FirstName = client.FirstName,
            LastName = client.LastName,
            Pesel = client.Pesel,
            Address = client.Address,
            Email = client.Email,
            PhoneNumber = client.PhoneNumber
        };
    }

    private static ClientResponse MapCompany(
        CompanyClient client)
    {
        return new ClientResponse
        {
            Id = client.Id,
            ClientType = "Company",
            CompanyName = client.CompanyName,
            Krs = client.Krs,
            Address = client.Address,
            Email = client.Email,
            PhoneNumber = client.PhoneNumber
        };
    }
}