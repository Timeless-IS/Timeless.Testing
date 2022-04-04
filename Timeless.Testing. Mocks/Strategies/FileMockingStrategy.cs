using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Timeless.Testing.Mocks
{
    public class FileMockingStrategy : IMockingStrategy
    {
        private readonly DirectoryInfo dir;

        public FileMockingStrategy(DirectoryInfo dir)
        {
            this.dir = dir;
        }

        public async Task MockResponse(HttpContext ctx)
        {
            var path = ctx.Request.Path.HasValue ? ctx.Request.Path.Value.Trim('/') : String.Empty;

            var defaultDirectory = new DirectoryInfo(Path.Combine(dir.FullName, "responses", path));

            // get file path from request headers or default and add to response headers
            var filePath = "";

            if (ctx.Request.Headers.ContainsKey("X-Response-FilePath"))
            {
                filePath = ctx.Request.Headers["X-Response-FilePath"].ToString();
            }
            else if (defaultDirectory.Exists)
            {
                filePath = defaultDirectory.GetFiles().FirstOrDefault()?.FullName.Replace(dir.FullName, "");
            }

            if (String.IsNullOrEmpty(filePath))
            {
                return;
            }

            filePath = filePath.Trim(Path.DirectorySeparatorChar).Trim(Path.AltDirectorySeparatorChar);

            var responseContent = await File.ReadAllBytesAsync(Path.Combine(dir.FullName, filePath));

            BinaryData.FromBytes(responseContent).ToHttpResponse(ctx);

            ctx.Response.Headers["X-Response-FilePath"] = filePath;
        }
    }
}
