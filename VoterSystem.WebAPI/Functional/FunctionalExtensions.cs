using Microsoft.AspNetCore.Mvc;
using VoterSystem.Shared.Functional;

namespace VoterSystem.WebAPI.Functional;

public sealed class ErrorDto
{
    public required string Message { get; init; }
    public required string? InnerMessage { get; init; }
}

public static class FunctionalExtensions
{
    private static ErrorDto ToErrorDto(this ServiceError error)
    {
        return new ErrorDto
        {
            Message = error.Message,
            InnerMessage = error.Exception?.Message
        };
    }
    
    public static IActionResult ToHttpResult(this ServiceError error)
    {
        return error switch
        {
            NotFoundError nfe => new NotFoundObjectResult(nfe.ToErrorDto()),
            BadRequestError bre => new BadRequestObjectResult(bre.ToErrorDto()),
            ConflictError conf => new ConflictObjectResult(conf.ToErrorDto()),
            UnauthorizedError un => new UnauthorizedObjectResult(un.ToErrorDto()),
            UnprocessableEntityError uee => new UnprocessableEntityObjectResult(uee.ToErrorDto()),
            _ => new BadRequestResult()
        };
    }

    public static IActionResult ToHttpResult<T, TE>(this Result<T, TE> result, Func<T, IActionResult> valueAction)
        where TE : ServiceError
    {
        return result.Map(valueAction, e => e.ToHttpResult());
    }

    public static IActionResult ToOkResult<T, TR, TE>(this Result<T, TE> result, Func<T, TR> valueAction)
        where TE : ServiceError
    {
        return result.Map(v => new OkObjectResult(valueAction(v)), e => e.ToHttpResult());
    }

    public static IActionResult ToHttpResult<T, TE>(this Result<T, TE> result)
        where TE : ServiceError
    {
        return result.Map(v => new OkObjectResult(v), e => e.ToHttpResult());
    }

    public static IActionResult ToHttpResult<TE>(this Option<TE> option)
        where TE : ServiceError
    {
        return option.Map<IActionResult>(e => e.ToHttpResult(),
            () => new OkResult());
    }
}