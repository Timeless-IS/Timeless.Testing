using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Timeless.Testing.Mocks
{
    public class BlobMockingStrategy : IMockingStrategy
    {
        private readonly BlobContainerClient blb;

        public BlobMockingStrategy(BlobContainerClient blb)
        {
            this.blb = blb;
        }

        public async Task MockResponse(HttpContext ctx)
        {
            var path = ctx.Request.Path.HasValue ? ctx.Request.Path.Value.Trim('/') : String.Empty;

            // get blob name from request headers or default and add to response headers
            var blobName = ctx.Request.Headers.ContainsKey("X-Response-BlobName")
                ? ctx.Request.Headers["X-Response-BlobName"].ToString()
                : blb.GetBlobs(prefix: $"responses/{path}").FirstOrDefault()?.Name
                ;

            if (String.IsNullOrEmpty(blobName))
            {
                return;
            }

            var blobClient = blb.GetBlobClient(blobName);

            var downloadResponse = await blobClient.DownloadContentAsync();

            downloadResponse.Value.Content.ToHttpResponse(ctx);

            ctx.Response.Headers["X-Response-BlobName"] = blobName;
            ctx.Response.Headers["X-Response-DownloadStatus"] = downloadResponse.GetRawResponse().Status.ToString();
        }
    }
}
