using Markadan.API.Filters;
using Markadan.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Tests.Filters;

public sealed class ApiExceptionFilterTests
{
    private static ExceptionContext MakeContext(Exception ex)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ExceptionContext(actionContext, []) { Exception = ex };
    }

    [Fact]
    public void BusinessRuleException_Returns409()
    {
        var ctx = MakeContext(new BusinessRuleException("iş kuralı ihlali"));
        new ApiExceptionFilter().OnException(ctx);
        Assert.Equal(409, ((ObjectResult)ctx.Result!).StatusCode);
    }

    [Fact]
    public void KeyNotFoundException_Returns404()
    {
        var ctx = MakeContext(new KeyNotFoundException("kayıt yok"));
        new ApiExceptionFilter().OnException(ctx);
        Assert.Equal(404, ((ObjectResult)ctx.Result!).StatusCode);
    }

    [Fact]
    public void ArgumentException_Returns400()
    {
        var ctx = MakeContext(new ArgumentException("geçersiz parametre"));
        new ApiExceptionFilter().OnException(ctx);
        Assert.Equal(400, ((ObjectResult)ctx.Result!).StatusCode);
    }

    [Fact]
    public void DbUpdateException_Returns409()
    {
        var ctx = MakeContext(new DbUpdateException("FK/unique ihlali", (Exception?)null));
        new ApiExceptionFilter().OnException(ctx);
        Assert.Equal(409, ((ObjectResult)ctx.Result!).StatusCode);
    }
}
