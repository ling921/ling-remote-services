namespace Ling.RemoteServices.Exceptions;

public abstract class RemoteApiException : Exception
{
}

public class RemoteValidationException : RemoteApiException
{
}

public class RemoteUnauthorizedException : RemoteApiException
{
}

public class RemoteForbiddenException : RemoteApiException
{
}

public class RemoteNotFoundException : RemoteApiException
{
}

public class RemoteInternalServerErrorException : RemoteApiException
{
}
