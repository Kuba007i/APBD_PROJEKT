namespace RevenueRecognition.Api.Common.Exceptions;

public sealed class BusinessRuleException : AppException
{
    public BusinessRuleException(string message)
        : base(message, StatusCodes.Status400BadRequest)
    {
    }
}