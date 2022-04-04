using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Timeless.Testing.Mocks
{
    public static class HttpResponseExtensions
    {
        public static DynamicTableEntity ToTableEntity(this HttpResponse res)
        {
            // FIXME: this will add the Host header from the requets to the collection...
            var req = res.HttpContext.Request;

            var pk = req.HttpContext.Connection.Id;
            var rk = Guid.NewGuid().ToString();

            var dte = new DynamicTableEntity(pk, rk);

            dte.Properties["Method"] = EntityProperty.GeneratePropertyForString(req.Method);
            dte.Properties["Scheme"] = EntityProperty.GeneratePropertyForString(req.Scheme);
            dte.Properties["Host"] = EntityProperty.GeneratePropertyForString(req.Host.HasValue ? req.Host.Value : null);
            dte.Properties["Path"] = EntityProperty.GeneratePropertyForString(req.Path.HasValue ? req.Path.Value : null);
            dte.Properties["QueryString"] = EntityProperty.GeneratePropertyForString(req.QueryString.HasValue ? req.QueryString.Value : null);
            dte.Properties["Protocol"] = EntityProperty.GeneratePropertyForString(req.Protocol);

            dte.Properties["StatusCode"] = EntityProperty.GeneratePropertyForInt(res.StatusCode);

            // calling ToString() on StringValues will join multiple values into a single comma separated string
            var headersString = JsonConvert.SerializeObject(res.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()));

            dte.Properties["Headers"] = EntityProperty.GeneratePropertyForString(headersString);

            if (res.Body != null && res.Body.Position > 0)
            {
                res.Body.Position = 0;
            }

            dte.Properties["Content"] = EntityProperty.GeneratePropertyForByteArray(BinaryData.FromStream(res.Body).ToArray());

            return dte;
        }

        public static BinaryData ToBinaryData(this HttpResponse res)
        {
            var req = res.HttpContext.Request;

            // see also HttpStatusCode enum?
            var contentBuilder = new StringBuilder($"{req.Protocol} {res.StatusCode} {ReasonPhrases.GetReasonPhrase(res.StatusCode)}\r\n");

            foreach (var h in res.Headers)
            {
                contentBuilder.AppendLine($"{h.Key}: {h.Value.ToString()}");
            }

            contentBuilder.Append($"{Environment.NewLine}{BinaryData.FromStream(res.Body)}");

            return BinaryData.FromString(contentBuilder.ToString());
        }

        public static void ToHttpResponse(this DynamicTableEntity dte, HttpContext ctx)
        {
            // map request + add headers, content and status code
            dte.ToHttpRequest(ctx);
            var res = ctx.Response;

            if (dte.Properties.ContainsKey("StatusCode"))
            {
                res.StatusCode = dte.Properties["StatusCode"].Int32Value.Value;
            }

            var headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(dte.Properties["Headers"].StringValue);

            foreach (var h in headers)
            {
                res.Headers[h.Key] = h.Value;
            }

            if (dte.Properties.ContainsKey("Content"))
            {
                res.Body = BinaryData.FromBytes(dte.Properties["Content"].BinaryValue).ToStream();
            }
        }

        public static void ToHttpResponse(this BinaryData bin, HttpContext ctx)
        {
            var lines = bin.ToString().Split(Environment.NewLine);

            var summary = lines.FirstOrDefault();

            // match and capture anything that is not whitespace - returns 2 or more captures: protocol/version, status code (int and text, which can contain spaces)
            var captures = Regex.Matches(summary, "([\\S]+)");

            if (captures.Count < 2)
            {
                throw new Exception($"Summary line is malformed: '{summary}'");
            }

            ctx.Response.StatusCode = Int32.Parse(captures[1].Value);

            ctx.Request.Protocol = captures[0].Value;

            foreach (var l in lines.Skip(1))
            {
                if (l == String.Empty)
                {
                    // headers finished, content starting
                    break;
                }

                var header = l.Split(':', 2);

                ctx.Response.Headers.Add(header[0], header[1].TrimStart());
            }

            var contentBuilder = new StringBuilder();

            foreach (var l in lines.Skip(1 + ctx.Response.Headers.Count + 1))
            {
                contentBuilder.AppendLine(l);
            }

            ctx.Response.Body = BinaryData.FromString(contentBuilder.ToString().TrimEnd()).ToStream();
        }
    }
}
