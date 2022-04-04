using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Timeless.Testing.Mocks
{
    public static class HttpRequestExtensions
    {
        public static DynamicTableEntity ToTableEntity(this HttpRequest req)
        {
            var pk = req.HttpContext.Connection.Id;
            var rk = Guid.NewGuid().ToString();

            var dte = new DynamicTableEntity(pk, rk);

            dte.Properties["Method"] = EntityProperty.GeneratePropertyForString(req.Method);
            dte.Properties["Scheme"] = EntityProperty.GeneratePropertyForString(req.Scheme);
            dte.Properties["Host"] = EntityProperty.GeneratePropertyForString(req.Host.HasValue ? req.Host.Value : null);
            dte.Properties["Path"] = EntityProperty.GeneratePropertyForString(req.Path.HasValue ? req.Path.Value : null);
            dte.Properties["QueryString"] = EntityProperty.GeneratePropertyForString(req.QueryString.HasValue ? req.QueryString.Value : null);
            dte.Properties["Protocol"] = EntityProperty.GeneratePropertyForString(req.Protocol);

            // not supported
            //if (req.Body != null && req.Body.Position > 0)
            //{
            //    req.Body.Position = 0;
            //}

            dte.Properties["Content"] = EntityProperty.GeneratePropertyForByteArray(BinaryData.FromStream(req.Body).ToArray());

            // calling ToString() on StringValues will join multiple values into a single comma separated string
            var headersString = JsonConvert.SerializeObject(req.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()));

            dte.Properties["Headers"] = EntityProperty.GeneratePropertyForString(headersString);

            return dte;
        }

        public static BinaryData ToBinaryData(this HttpRequest req)
        {
            // TODO: guard
            var host = req.Host.Value.Contains(":") ? req.Host.Value.Split(":")[0] : req.Host.Value;
            var port = req.Host.Value.Contains(":") ? req.Host.Value.Split(":")[1] : "80";

            var url = new UriBuilder(req.Scheme, host, Int32.Parse(port));
            url.Path = req.Path;
            url.Query = req.QueryString.HasValue ? req.QueryString.Value : default;

            var contentBuilder = new StringBuilder($"{req.Method} {url} {req.Protocol}\r\n");

            foreach (var h in req.Headers)
            {
                contentBuilder.AppendLine($"{h.Key}: {h.Value.ToString()}");
            }

            // not supported
            //if (req.Body != null && req.Body.Position > 0)
            //{
            //    req.Body.Position = 0;
            //}

            contentBuilder.Append($"{Environment.NewLine}{BinaryData.FromStream(req.Body)}");

            return BinaryData.FromString(contentBuilder.ToString());
        }

        public static void ToHttpRequest(this DynamicTableEntity dte, HttpContext ctx)
        {
            ctx.Connection.Id = dte.PartitionKey;
            ctx.Request.Method = dte.Properties["Method"].StringValue;
            ctx.Request.Scheme = dte.Properties["Scheme"].StringValue;
            ctx.Request.Host = new HostString(dte.Properties["Host"].StringValue);
            ctx.Request.Path = new PathString(dte.Properties["Path"].StringValue);
            ctx.Request.Protocol = dte.Properties["Protocol"].StringValue;

            ctx.Request.QueryString = dte.Properties.ContainsKey("QueryString")
                ? new QueryString(dte.Properties["QueryString"].StringValue)
                : QueryString.Empty;

            ctx.Request.Body = dte.Properties.ContainsKey("Content")
                ? BinaryData.FromBytes(dte.Properties["Content"].BinaryValue).ToStream()
                : default;

            var headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(dte.Properties["Headers"].StringValue);

            foreach (var h in headers)
            {
                ctx.Request.Headers[h.Key] = h.Value;
            }
        }

        public static void ToHttpRequest(this BinaryData bin, HttpContext ctx)
        {
            var lines = bin.ToString().Split(Environment.NewLine);

            var summary = lines.FirstOrDefault();

            // match and capture anything that is not whitespace - returns 3 captures: method, url and protocol/version
            var captures = Regex.Matches(summary, "([\\S]+)");

            if (captures.Count < 3)
            {
                throw new Exception($"Summary line is malformed: '{summary}'");
            }

            ctx.Request.Method = captures[0].Value;

            var url = new Uri(captures[1].Value);

            // TODO: test absolute path + host with/without port?
            ctx.Request.Scheme = url.Scheme;
            ctx.Request.Host = new HostString(url.Host, url.Port);
            ctx.Request.Path = new PathString(url.AbsolutePath);
            ctx.Request.QueryString = new QueryString(url.Query);

            ctx.Request.Protocol = captures[2].Value;

            foreach (var l in lines.Skip(1))
            {
                if (l == String.Empty)
                {
                    // headers finished, content starting
                    break;
                }

                var header = l.Split(':', 2);

                ctx.Request.Headers[header[0]] = header[1].TrimStart();
            }

            var contentBuilder = new StringBuilder();

            foreach (var l in lines.Skip(1 + ctx.Request.Headers.Count + 1))
            {
                contentBuilder.AppendLine(l);
            }

            ctx.Request.Body = BinaryData.FromString(contentBuilder.ToString().TrimEnd()).ToStream();
        }
    }
}
