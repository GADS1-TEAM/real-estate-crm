using System;
namespace PropertyService.Application;
public class PropertyDomainException : Exception
{
    public string ErrorCode { get; }
    public int HttpStatus { get; }
    public PropertyDomainException(string errorCode, int httpStatus, string message) : base(message)
    {
        ErrorCode = errorCode;
        HttpStatus = httpStatus;
    }
}
