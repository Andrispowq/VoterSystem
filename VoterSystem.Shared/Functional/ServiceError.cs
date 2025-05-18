namespace VoterSystem.Shared.Functional;

public abstract class ServiceError(string message) : Error(message)
{
    public override string ToString() => $"ServiceError({Message})";
}

public class NotFoundError(string message) : ServiceError(message)
{
    public override string ToString() => $"NotFoundError({Message})";
}
public class BadRequestError(string message) : ServiceError(message)
{
    public override string ToString() => $"BadRequestError({Message})";
}
public class ConflictError(string message) : ServiceError(message)
{
    public override string ToString() => $"ConflictError({Message})";
}
public class UnauthorizedError(string message) : ServiceError(message)
{
    public override string ToString() => $"UnauthorizedError({Message})";
}
public class UnprocessableEntityError() : ServiceError("Unprocessable entity")
{
    public override string ToString() => $"UnprocessableEntityError({Message})";
}