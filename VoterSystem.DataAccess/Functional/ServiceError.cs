namespace VoterSystem.DataAccess.Functional;

public abstract class ServiceError(string message) : Error(message);

public class NotFoundError(string message) : ServiceError(message);
public class BadRequestError(string message) : ServiceError(message);
public class ConflictError(string message) : ServiceError(message);
public class UnauthorizedError(string message) : ServiceError(message);
public class UnprocessableEntityError() : ServiceError("Unprocessable entity");