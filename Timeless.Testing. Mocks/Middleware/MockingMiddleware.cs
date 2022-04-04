using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Timeless.Testing.Mocks
{
    public class MockingMiddleware
    {
        // singleton dependencies in the ctor, per-request dependencies go in the method signature
        private readonly RequestDelegate next;
        private readonly IMockingStrategy mock;

        public MockingMiddleware(RequestDelegate next, IMockingStrategy mock)
        {
            this.next = next;
            this.mock = mock;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            await next(ctx);

            await mock.MockResponse(ctx);
        }
    }
}
