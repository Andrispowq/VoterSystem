using Microsoft.AspNetCore.Mvc;
using VoterSystem.DataAccess.Functional;

namespace VoterSystem.WebAPI.Functional;

public static class FunctionalExtensions
{
    public static IActionResult ToHttpResult(this ServiceError error)
    {
        return error switch
        {
            NotFoundError nfe => new NotFoundObjectResult(nfe.Message),
            BadRequestError bre => new BadRequestObjectResult(bre.Message),
            ConflictError conf => new ConflictObjectResult(conf.Message),
            UnauthorizedError un => new UnauthorizedObjectResult(un.Message),
            UnprocessableEntityError uee => new UnprocessableEntityObjectResult(uee.Message),
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