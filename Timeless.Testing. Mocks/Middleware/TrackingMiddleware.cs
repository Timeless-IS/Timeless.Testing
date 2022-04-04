using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Timeless.Testing.Mocks
{
    public class TrackingMiddleware
    {
        // singleton dependencies in the ctor, per-request dependencies go in the method signature
        private readonly RequestDelegate next;
        private readonly ITrackingStrategy track;

        public TrackingMiddleware(RequestDelegate next, ITrackingStrategy track)
        {
            this.next = next;
            this.track = track;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            await track.TrackRequest(ctx);

            await next(ctx);
        }
    }
}
