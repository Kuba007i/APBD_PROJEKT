using RevenueRecognition.Api.Contracts.Payments;

namespace RevenueRecognition.Api.Services.Payments;

public interface IContractPaymentService
{
    Task<ContractPaymentResponse> CreateAsync(
        int contractId,
        CreateContractPaymentRequest request);
}