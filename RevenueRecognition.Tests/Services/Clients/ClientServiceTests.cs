using Microsoft.EntityFrameworkCore;
using RevenueRecognition.Api.Common.Exceptions;
using RevenueRecognition.Api.Contracts.Clients;
using RevenueRecognition.Api.Domain.Entities;
using RevenueRecognition.Api.Services.Clients;
using RevenueRecognition.Tests.Helpers;

namespace RevenueRecognition.Tests.Services.Clients;

public sealed class ClientServiceTests
{
    [Fact]
    public async Task CreateIndividualAsync_ShouldCreateClient()
    {
        // Arrange
        await using var dbContext =
            TestDbContextFactory.Create();

        var service =
            new ClientService(dbContext);

        var request =
            new CreateIndividualClientRequest
            {
                FirstName = "Jan",
                LastName = "Kowalski",
                Address = "Warszawa",
                Email = "jan@example.com",
                PhoneNumber = "500600700",
                Pesel = "99010112345"
            };

        // Act
        var response =
            await service.CreateIndividualAsync(request);

        // Assert
        Assert.True(response.Id > 0);
        Assert.Equal("Individual", response.ClientType);
        Assert.Equal("Jan", response.FirstName);
        Assert.Equal("Kowalski", response.LastName);
        Assert.Equal("99010112345", response.Pesel);

        var savedClient =
            await dbContext.IndividualClients
                .SingleAsync();

        Assert.Equal("Jan", savedClient.FirstName);
        Assert.False(savedClient.IsDeleted);
    }
    
    [Fact]
    public async Task CreateIndividualAsync_WhenPeselExists_ShouldThrowConflictException()
    {
        // Arrange
        await using var dbContext =
            TestDbContextFactory.Create();

        dbContext.IndividualClients.Add(
            new IndividualClient
            {
                FirstName = "Anna",
                LastName = "Nowak",
                Address = "Kraków",
                Email = "anna@example.com",
                PhoneNumber = "111222333",
                Pesel = "99010112345"
            });

        await dbContext.SaveChangesAsync();

        var service =
            new ClientService(dbContext);

        var request =
            new CreateIndividualClientRequest
            {
                FirstName = "Jan",
                LastName = "Kowalski",
                Address = "Warszawa",
                Email = "jan@example.com",
                PhoneNumber = "500600700",
                Pesel = "99010112345"
            };

        // Act
        var action = async () =>
            await service.CreateIndividualAsync(request);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);

        Assert.Equal(
            1,
            await dbContext.IndividualClients.CountAsync());
    }
    
    [Fact]
    public async Task DeleteAsync_ForIndividual_ShouldAnonymizeAndSoftDeleteClient()
    {
        // Arrange
        await using var dbContext =
            TestDbContextFactory.Create();

        var client =
            new IndividualClient
            {
                FirstName = "Jan",
                LastName = "Kowalski",
                Address = "Warszawa",
                Email = "jan@example.com",
                PhoneNumber = "500600700",
                Pesel = "99010112345"
            };

        dbContext.IndividualClients.Add(client);

        await dbContext.SaveChangesAsync();

        var service =
            new ClientService(dbContext);

        // Act
        await service.DeleteAsync(client.Id);

        // Assert
        var savedClient =
            await dbContext.IndividualClients
                .SingleAsync();

        Assert.True(savedClient.IsDeleted);
        Assert.Equal("DELETED", savedClient.FirstName);
        Assert.Equal("DELETED", savedClient.LastName);
        Assert.Equal("DELETED", savedClient.Address);
        Assert.Equal("DELETED", savedClient.PhoneNumber);

        Assert.NotEqual(
            "99010112345",
            savedClient.Pesel);

        Assert.EndsWith(
            "@deleted.local",
            savedClient.Email);
    }
    
    [Fact]
    public async Task DeleteAsync_ForCompany_ShouldThrowBusinessRuleException()
    {
        // Arrange
        await using var dbContext =
            TestDbContextFactory.Create();

        var company =
            new CompanyClient
            {
                CompanyName = "Test Company",
                Address = "Warszawa",
                Email = "company@example.com",
                PhoneNumber = "221234567",
                Krs = "0000123456"
            };

        dbContext.CompanyClients.Add(company);

        await dbContext.SaveChangesAsync();

        var service =
            new ClientService(dbContext);

        // Act
        var action = async () =>
            await service.DeleteAsync(company.Id);

        // Assert
        var exception =
            await Assert.ThrowsAsync<
                BusinessRuleException>(action);

        Assert.Equal(
            "Dane firmy nie mogą zostać usunięte.",
            exception.Message);

        var savedCompany =
            await dbContext.CompanyClients
                .SingleAsync();

        Assert.False(savedCompany.IsDeleted);
    }
}