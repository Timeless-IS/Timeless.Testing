using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Timeless.Testing.Mocks
{
    public class BlobTrackingStrategy : ITrackingStrategy
    {
        private readonly BlobContainerClient blb;

        public BlobTrackingStrategy(BlobContainerClient blb)
        {
            this.blb = blb;
        }

        public async Task TrackRequest(HttpContext ctx)
        {
            var path = ctx.Request.Path.HasValue ? ctx.Request.Path.Value.Trim('/') : String.Empty;

            // get blob name from request headers or default and add to response headers
            var blobName = ctx.Request.Headers.ContainsKey("X-Request-BlobName")
                ? ctx.Request.Headers["X-Request-BlobName"].ToString()
                : $"requests/{path}/{Guid.NewGuid()}.txt"
                ;

            var blobClient = blb.GetBlobClient(blobName);

            var uploadResponse = await blobClient.UploadAsync(ctx.Request.ToBinaryData());

            blobClient.SetHttpHeaders(new BlobHttpHeaders { ContentType = "text/plain" });

            ctx.Response.Headers["X-Request-BlobName"] = blobName;
            ctx.Response.Headers["X-Request-UploadStatus"] = uploadResponse.GetRawResponse().Status.ToString();
        }
    }
}
