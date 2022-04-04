using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Timeless.Testing.Mocks
{
    public interface IMockingStrategy
    {
        Task MockResponse(HttpContext ctx);
    }
}
