namespace RevenueRecognition.Api.Common.Exceptions;

public sealed class ExternalServiceException : AppException
{
    public ExternalServiceException(string message)
        : base(message, StatusCodes.Status502BadGateway)
    {
    }
}