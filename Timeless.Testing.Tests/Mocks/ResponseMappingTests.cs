using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Timeless.Testing.Mocks;
using Xunit;

namespace Timeless.Testing.Tests
{
    public class ResponseMappingTests
    {
        [Fact(DisplayName = "Response to TableEntity and back")]
        public void Test01()
        {
            var sourceContext = new DefaultHttpContext();

            sourceContext.CreateResponse("POST", "https://localhost:5001/customers?id=123&name=mario", 201);

            sourceContext.Request.Headers["Content-Type"] = "application/json";
            sourceContext.Request.Headers["Connection"] = "keep-alive";

            sourceContext.Response.Headers["Content-Type"] = "application/json";
            sourceContext.Response.Headers["CorrelationId"] = "Correlation1";

            sourceContext.Request.Body = BinaryData.FromString("{\"message\":\"Hello request!\"}").ToStream();
            sourceContext.Response.Body = BinaryData.FromString("{\"message\":\"Hello response!\"}").ToStream();

            #region ToTableEntity()

            var dte = sourceContext.Response.ToTableEntity();

            dte.PartitionKey.Should().Be(sourceContext.Connection.Id);
            dte.Properties["Method"].StringValue.Should().Be(sourceContext.Request.Method);
            dte.Properties["Scheme"].StringValue.Should().Be(sourceContext.Request.Scheme);
            dte.Properties["Host"].StringValue.Should().Be(sourceContext.Request.Host.Value);
            dte.Properties["Path"].StringValue.Should().Be(sourceContext.Request.Path.Value);
            dte.Properties["QueryString"].StringValue.Should().Be(sourceContext.Request.QueryString.Value);
            dte.Properties["Protocol"].StringValue.Should().Be(sourceContext.Request.Protocol);

            sourceContext.Response.Body.Position = 0;

            var responseBody = BinaryData.FromStream(sourceContext.Response.Body).ToString();
            var entityContent = BinaryData.FromBytes(dte.Properties["Content"].BinaryValue).ToString();

            entityContent.Should().Be(responseBody);

            var headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(dte.Properties["Headers"].StringValue);

            headers.Should().BeEquivalentTo(sourceContext.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()));

            #endregion

            #region ToHttpResponse()

            // table entity was mapped correctly, test reverse mapping...
            var targetContext = new DefaultHttpContext();

            dte.ToHttpResponse(targetContext);

            targetContext.Connection.Id.Should().Be(sourceContext.Connection.Id);
            targetContext.Request.Method.Should().Be(sourceContext.Request.Method);
            targetContext.Request.Scheme.Should().Be(sourceContext.Request.Scheme);
            targetContext.Request.Host.Should().Be(sourceContext.Request.Host);
            targetContext.Request.Path.Should().Be(sourceContext.Request.Path);
            targetContext.Request.QueryString.Should().Be(sourceContext.Request.QueryString);
            targetContext.Request.Protocol.Should().Be(sourceContext.Request.Protocol);

            targetContext.Response.StatusCode.Should().Be(sourceContext.Response.StatusCode);

            var bodyFromMapping = BinaryData.FromStream(targetContext.Response.Body).ToString();

            bodyFromMapping.Should().Be(responseBody);

            targetContext.Response.Headers.Should().BeEquivalentTo(sourceContext.Response.Headers);

            #endregion
        }

        [Fact(DisplayName = "Response to BinaryData and back")]
        public void Test02()
        {
            var sourceContext = new DefaultHttpContext();

            sourceContext.CreateResponse("POST", "https://localhost:5001/customers?id=123&name=mario", 201);

            sourceContext.Request.Headers["Content-Type"] = "application/json";
            sourceContext.Request.Headers["Connection"] = "keep-alive";

            sourceContext.Response.Headers["Content-Type"] = "application/json";
            sourceContext.Response.Headers["CorrelationId"] = "Correlation1";

            sourceContext.Request.Body = BinaryData.FromString("{\"message\":\"Hello request!\"}").ToStream();
            sourceContext.Response.Body = BinaryData.FromString("{\"message\":\"Hello response!\"}").ToStream();

            #region ToBinaryData()

            var bin = sourceContext.Response.ToBinaryData();

            var lines = bin.ToString().Split(Environment.NewLine);

            var summary = lines.FirstOrDefault();

            // first line has protocol and status code
            var captures = Regex.Matches(summary, "([\\S]+)");

            captures.Count.Should().Be(3);

            sourceContext.Response.StatusCode.Should().Be(Int32.Parse(captures[1].Value));
            sourceContext.Request.Protocol.Should().Be(captures[0].Value);

            var headers = lines.Skip(1).Take(sourceContext.Response.Headers.Count);

            foreach (var h in sourceContext.Response.Headers)
            {
                headers.Single(l => l == $"{h.Key}: {h.Value}");
            }

            sourceContext.Response.Body.Position = 0;

            var responseBody = lines.Skip(1 + headers.Count() + 1).Aggregate((l1, l2) => $"{l1}{Environment.NewLine}{l2}");

            responseBody.Should().Be(BinaryData.FromStream(sourceContext.Response.Body).ToString());

            #endregion

            #region ToHttpResponse()

            // binary data was mapped correctly, test reverse mapping...
            var targetContext = new DefaultHttpContext();

            bin.ToHttpResponse(targetContext);

            //targetContext.Connection.Id.Should().Be(sourceContext.Connection.Id);
            //targetContext.Request.Method.Should().Be(sourceContext.Request.Method);
            //targetContext.Request.Scheme.Should().Be(sourceContext.Request.Scheme);
            //targetContext.Request.Host.Should().Be(sourceContext.Request.Host);
            //targetContext.Request.Path.Should().Be(sourceContext.Request.Path);
            //targetContext.Request.QueryString.Should().Be(sourceContext.Request.QueryString);
            targetContext.Request.Protocol.Should().Be(sourceContext.Request.Protocol);

            targetContext.Response.StatusCode.Should().Be(sourceContext.Response.StatusCode);

            var bodyFromMapping = BinaryData.FromStream(targetContext.Response.Body).ToString();

            bodyFromMapping.Should().Be(responseBody);

            targetContext.Response.Headers.Should().BeEquivalentTo(sourceContext.Response.Headers);

            #endregion
        }
    }
}
