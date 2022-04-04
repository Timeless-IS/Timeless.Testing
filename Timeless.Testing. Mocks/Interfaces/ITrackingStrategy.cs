using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Timeless.Testing.Mocks
{
    public interface ITrackingStrategy
    {
        Task TrackRequest(HttpContext ctx);
    }
}
