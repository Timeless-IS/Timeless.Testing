using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Threading.Tasks;

namespace Timeless.Testing.Mocks
{
    public class TableMockingStrategy : IMockingStrategy
    {
        private readonly CloudTable tbl;

        public TableMockingStrategy(CloudTable tbl)
        {
            this.tbl = tbl;
        }

        public async Task MockResponse(HttpContext ctx)
        {
            if (ctx.Request.Headers.ContainsKey("X-Response-PartitionKey") == false && 
                ctx.Request.Headers.ContainsKey("X-Response-RowKey") == false)
            {
                // if both headers are missing, okay (just return default 200)
                return;
            }

            if (ctx.Request.Headers.ContainsKey("X-Response-PartitionKey") == false || 
                ctx.Request.Headers.ContainsKey("X-Response-RowKey") == false)
            {
                // if only one header is missing, error
                throw new ArgumentException("This operation requires both X-Response-PartitionKey and X-Response-RowKey headers or neither");
            }

            var pk = ctx.Request.Headers["X-Response-PartitionKey"];
            var rk = ctx.Request.Headers["X-Response-RowKey"];

            var retrieveOperation = TableOperation.Retrieve(pk, rk);

            var retrieveResponse = await tbl.ExecuteAsync(retrieveOperation);

            var dte = (retrieveResponse.Result as DynamicTableEntity);

            if (dte != null)
            {
                ctx.Response.Headers["X-Response-PartitionKey"] = dte.PartitionKey;
                ctx.Response.Headers["X-Response-RowKey"] = dte.RowKey;
            }

            ctx.Response.Headers["X-Response-RetrieveStatus"] = retrieveResponse.HttpStatusCode.ToString();
        }
    }
}
