namespace Asisya.Application.Common;

public sealed class BusinessException : Exception
{
    public BusinessException(string message) : base(message)
    {
    }
}
