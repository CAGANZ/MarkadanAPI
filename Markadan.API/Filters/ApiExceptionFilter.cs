using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Markadan.API.Filters;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var ex = context.Exception;
        int status;
        string title;

        if (ex is DbUpdateException)
        {
            status = StatusCodes.Status409Conflict;
            title = "Conflict";
        }
        else if (ex is InvalidOperationException ioe && ioe.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            status = StatusCodes.Status404NotFound;
            title = "Not Found";
        }
        else if (ex is InvalidOperationException)
        {
            status = StatusCodes.Status400BadRequest;
            title = "Bad Request";
        }
        else
        {
            status = StatusCodes.Status500InternalServerError;
            title = "Internal Server Error";
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = ex.Message
        };

        context.Result = new ObjectResult(problem) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
