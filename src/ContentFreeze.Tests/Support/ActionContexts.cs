using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace ContentFreeze.Tests.Support;

internal static class ActionContexts
{
    public static ControllerActionDescriptor Action<TController>() => new()
    {
        ControllerTypeInfo = typeof(TController).GetTypeInfo(),
        ControllerName = typeof(TController).Name,
    };

    public static HttpRequest Request(string method)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        return httpContext.Request;
    }

    public static ActionExecutingContext Executing<TController>(string method)
    {
        var actionContext = new ActionContext(Request(method).HttpContext, new RouteData(), Action<TController>());
        return new ActionExecutingContext(actionContext, [], new Dictionary<string, object?>(), controller: new object());
    }
}
