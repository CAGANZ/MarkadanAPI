using Markadan.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Markadan.API.Filters
{
    public sealed class ApiExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var ex = context.Exception;

            var (status, title) = ex switch
            {
                BusinessRuleException => (HttpStatusCode.Conflict, "Business rule violated"), // 409
                KeyNotFoundException => (HttpStatusCode.NotFound, "Not Found"),              // 404
                InvalidOperationException => (HttpStatusCode.BadRequest, "Bad Request"),            // 400
                ArgumentException => (HttpStatusCode.BadRequest, "Bad Request"),            // 400
                DbUpdateException => (HttpStatusCode.Conflict, "Conflict"),               // 409 (FK/unique)
                _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
            };

            var problem = new ProblemDetails
            {
                Status = (int)status,
                Title = title,
                Detail = ex.Message
            };

            context.Result = new ObjectResult(problem) { StatusCode = (int)status };
            context.ExceptionHandled = true;
        }
    }
}
