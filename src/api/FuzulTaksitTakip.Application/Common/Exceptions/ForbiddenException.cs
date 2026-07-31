namespace FuzulTaksitTakip.Application.Common.Exceptions;

public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message = "You are not allowed to access this resource.")
        : base(message)
    {
    }
}
