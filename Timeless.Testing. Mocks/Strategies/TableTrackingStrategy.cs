using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Table;
using System.Threading.Tasks;

namespace Timeless.Testing.Mocks
{
    public class TableTrackingStrategy : ITrackingStrategy
    {
        private readonly CloudTable tbl;

        public TableTrackingStrategy(CloudTable tbl)
        {
            this.tbl = tbl;
        }

        public async Task TrackRequest(HttpContext ctx)
        {
            var dte = ctx.Request.ToTableEntity();

            var insertOperation = TableOperation.Insert(dte);

            var insertResponse = await tbl.ExecuteAsync(insertOperation);

            ctx.Response.Headers["X-Request-PartitionKey"] = dte.PartitionKey;
            ctx.Response.Headers["X-Request-RowKey"] = dte.RowKey;
            ctx.Response.Headers["X-Request-InsertStatus"] = insertResponse.HttpStatusCode.ToString();
        }
    }
}
