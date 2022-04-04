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
    public class RequestMappingTests
    {
        [Fact(DisplayName = "Request to TableEntity and back")]
        public void Test01()
        {
            var sourceContext = new DefaultHttpContext();

            sourceContext.CreateRequest("POST", "https://localhost:5001/customers?id=123&name=mario");

            sourceContext.Request.Headers["Content-Type"] = "application/json";
            sourceContext.Request.Headers["Connection"] = "keep-alive";

            sourceContext.Request.Body = BinaryData.FromString("{\"message\":\"Hello world!\"}").ToStream();

            #region ToTableEntity()

            var dte = sourceContext.Request.ToTableEntity();

            dte.PartitionKey.Should().Be(sourceContext.Connection.Id);
            dte.Properties["Method"].StringValue.Should().Be(sourceContext.Request.Method);
            dte.Properties["Scheme"].StringValue.Should().Be(sourceContext.Request.Scheme);
            dte.Properties["Host"].StringValue.Should().Be(sourceContext.Request.Host.Value);
            dte.Properties["Path"].StringValue.Should().Be(sourceContext.Request.Path.Value);
            dte.Properties["QueryString"].StringValue.Should().Be(sourceContext.Request.QueryString.Value);
            dte.Properties["Protocol"].StringValue.Should().Be(sourceContext.Request.Protocol);

            sourceContext.Request.Body.Position = 0;

            var requestBody = BinaryData.FromStream(sourceContext.Request.Body).ToString();
            var entityContent = BinaryData.FromBytes(dte.Properties["Content"].BinaryValue).ToString();

            entityContent.Should().Be(requestBody);

            var headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(dte.Properties["Headers"].StringValue);

            headers.Should().BeEquivalentTo(sourceContext.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()));

            #endregion

            #region ToHttpRequest()

            // table entity was mapped correctly, test reverse mapping...
            var targetContext = new DefaultHttpContext();

            dte.ToHttpRequest(targetContext);

            targetContext.Connection.Id.Should().Be(sourceContext.Connection.Id);
            targetContext.Request.Method.Should().Be(sourceContext.Request.Method);
            targetContext.Request.Scheme.Should().Be(sourceContext.Request.Scheme);
            targetContext.Request.Host.Should().Be(sourceContext.Request.Host);
            targetContext.Request.Path.Should().Be(sourceContext.Request.Path);
            targetContext.Request.QueryString.Should().Be(sourceContext.Request.QueryString);
            targetContext.Request.Protocol.Should().Be(sourceContext.Request.Protocol);

            var bodyFromMapping = BinaryData.FromStream(targetContext.Request.Body).ToString();

            bodyFromMapping.Should().Be(requestBody);

            targetContext.Request.Headers.Should().BeEquivalentTo(sourceContext.Request.Headers);

            #endregion
        }

        [Fact(DisplayName = "Request to BinaryData and back")]
        public void Test02()
        {
            var sourceContext = new DefaultHttpContext();

            sourceContext.CreateRequest("POST", "https://localhost:5001/customers?id=123&name=mario");

            sourceContext.Request.Headers["Content-Type"] = "application/json";
            sourceContext.Request.Headers["Connection"] = "keep-alive";

            sourceContext.Request.Body = BinaryData.FromString("{\"message\":\"Hello world!\"}").ToStream();

            #region ToBinaryData()

            var bin = sourceContext.Request.ToBinaryData();

            var lines = bin.ToString().Split(Environment.NewLine);

            // first line has method, url and protocol
            var summary = lines.First();

            var captures = Regex.Matches(summary, "([\\S]+)");

            captures.Count.Should().Be(3);

            captures[0].Value.Should().Be(sourceContext.Request.Method);

            var url = new Uri(captures[1].Value);

            url.Scheme.Should().Be(sourceContext.Request.Scheme);
            url.Host.Should().Be(sourceContext.Request.Host.Host);
            url.Port.Should().Be(sourceContext.Request.Host.Port.Value);
            url.AbsolutePath.Should().Be(sourceContext.Request.Path);
            url.Query.Should().Be(sourceContext.Request.QueryString.Value);

            captures[2].Value.Should().Be(sourceContext.Request.Protocol);

            var headers = lines.Skip(1).Take(sourceContext.Request.Headers.Count);

            foreach (var h in sourceContext.Request.Headers)
            {
                headers.Single(l => l == $"{h.Key}: {h.Value}");
            }

            sourceContext.Request.Body.Position = 0;

            var requestBody = lines.Skip(1 + headers.Count() + 1).Aggregate((l1, l2) => $"{l1}{Environment.NewLine}{l2}");

            requestBody.Should().Be(BinaryData.FromStream(sourceContext.Request.Body).ToString());

            #endregion

            #region ToHttpRequest()

            // binary data was mapped correctly, test reverse mapping...
            var targetContext = new DefaultHttpContext();

            bin.ToHttpRequest(targetContext);

            //targetContext.Connection.Id.Should().Be(sourceContext.Connection.Id);
            targetContext.Request.Method.Should().Be(sourceContext.Request.Method);
            targetContext.Request.Scheme.Should().Be(sourceContext.Request.Scheme);
            targetContext.Request.Host.Should().Be(sourceContext.Request.Host);
            targetContext.Request.Path.Should().Be(sourceContext.Request.Path);
            targetContext.Request.QueryString.Should().Be(sourceContext.Request.QueryString);
            targetContext.Request.Protocol.Should().Be(sourceContext.Request.Protocol);

            var bodyFromMapping = BinaryData.FromStream(targetContext.Request.Body).ToString();

            bodyFromMapping.Should().Be(requestBody);

            targetContext.Request.Headers.Should().BeEquivalentTo(sourceContext.Request.Headers);

            #endregion
        }
    }
}
