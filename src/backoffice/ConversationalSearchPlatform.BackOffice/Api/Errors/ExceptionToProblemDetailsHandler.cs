using System.Net;
using ConversationalSearchPlatform.BackOffice.Exceptions;

namespace ConversationalSearchPlatform.BackOffice.Api.Errors;

public class ExceptionToProblemDetailsHandler : Microsoft.AspNetCore.Diagnostics.IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public ExceptionToProblemDetailsHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            NotFoundException => HttpStatusCode.NotFound,
            BadHttpRequestException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };
        var castedCode = (int)statusCode;
        httpContext.Response.StatusCode = castedCode;
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Title = "An error occurred",
                Detail = exception.Message,
                Type = exception.GetType().Name,
                Status = castedCode
            },
            Exception = exception
        });
    }
}