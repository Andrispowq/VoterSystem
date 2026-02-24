namespace VoterSystem.Shared.Functional;

public abstract class ServiceError(string message, Exception? e) : Error(message, e)
{
    public override string ToString() => $"ServiceError({base.ToString()})";
}

public class NotFoundError(string message, Exception? e = null) : ServiceError(message, e)
{
    public override string ToString() => $"NotFoundError({base.ToString()})";
}
public class BadRequestError(string message, Exception? e = null) : ServiceError(message, e)
{
    public override string ToString() => $"BadRequestError({base.ToString()})";
}
public class ConflictError(string message, Exception? e = null) : ServiceError(message, e)
{
    public override string ToString() => $"ConflictError({base.ToString()})";
}
public class UnauthorizedError(string message, Exception? e = null) : ServiceError(message, e)
{
    public override string ToString() => $"UnauthorizedError({base.ToString()})";
}
public class UnprocessableEntityError(Exception? e = null) : ServiceError("Unprocessable entity", e)
{
    public override string ToString() => $"UnprocessableEntityError({base.ToString()})";
}