using RevenueRecognition.Api.Contracts.Revenue;

namespace RevenueRecognition.Api.Services.Revenue;

public interface IRevenueService
{
    Task<RevenueResponse> GetCurrentRevenueAsync(
        int? softwareId,
        string currency);

    Task<RevenueResponse> GetExpectedRevenueAsync(
        int? softwareId,
        string currency);
}