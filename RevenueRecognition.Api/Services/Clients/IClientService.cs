using RevenueRecognition.Api.Contracts.Clients;

namespace RevenueRecognition.Api.Services.Clients;

public interface IClientService
{
    Task<ClientResponse> CreateIndividualAsync(
        CreateIndividualClientRequest request);

    Task<ClientResponse> CreateCompanyAsync(
        CreateCompanyClientRequest request);

    Task<ClientResponse> UpdateIndividualAsync(
        int clientId,
        UpdateIndividualClientRequest request);

    Task<ClientResponse> UpdateCompanyAsync(
        int clientId,
        UpdateCompanyClientRequest request);

    Task DeleteAsync(int clientId);
}