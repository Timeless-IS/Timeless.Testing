using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Timeless.Testing.Mocks
{
    public class FileTrackingStrategy : ITrackingStrategy
    {
        private readonly DirectoryInfo dir;

        public FileTrackingStrategy(DirectoryInfo dir)
        {
            this.dir = dir;
        }

        public async Task TrackRequest(HttpContext ctx)
        {
            var path = ctx.Request.Path.HasValue ? ctx.Request.Path.Value.Trim('/') : String.Empty;

            // get file path (relative to configured root) from request headers or default and add to response headers
            var filePath = ctx.Request.Headers.ContainsKey("X-Request-FilePath")
                ? ctx.Request.Headers["X-Request-FilePath"].ToString()
                : $"requests/{path}/{Guid.NewGuid()}.txt"
                ;

            var fileInfo = new FileInfo(Path.Combine(dir.FullName, filePath));

            if (Directory.Exists(fileInfo.DirectoryName) == false)
            {
                Directory.CreateDirectory(fileInfo.DirectoryName);
            }

            await File.WriteAllBytesAsync(fileInfo.FullName, ctx.Request.ToBinaryData().ToArray());

            ctx.Response.Headers["X-Request-FilePath"] = filePath;
        }
    }
}
