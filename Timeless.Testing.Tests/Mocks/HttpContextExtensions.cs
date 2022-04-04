using Microsoft.AspNetCore.Http;
using System;

namespace Timeless.Testing.Tests
{
    public static class HttpContextExtensions
    {
        public static void CreateRequest(this HttpContext context, string method, string address)
        {
            context.Connection.Id = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            context.Request.Method = method;
            context.Request.Protocol = "HTTP/1.1";

            var uri = new Uri(address);

            context.Request.Host = new HostString(uri.Host, uri.Port);
            context.Request.Path = new PathString(uri.AbsolutePath);
            context.Request.QueryString = new QueryString(uri.Query);
            context.Request.Scheme = uri.Scheme;
        }

        public static void CreateResponse(this HttpContext context, string method, string address, int status = 200)
        {
            context.CreateRequest(method, address);

            context.Response.StatusCode = status;
        }
    }
}
