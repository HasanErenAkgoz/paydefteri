namespace PayDefteri.Application.Common.Exceptions;

public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Bu işlem için yetkiniz yok. Plan sahibi değilseniz veya üyeliğiniz yoksa erişemezsiniz.")
        : base(message)
    {
    }
}
