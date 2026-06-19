using RevenueRecognition.Api.Contracts.Contracts;

namespace RevenueRecognition.Api.Services.Contracts;

public interface IContractService
{
    Task<ContractResponse> CreateAsync(
        CreateContractRequest request);

    Task DeleteAsync(int contractId);
}