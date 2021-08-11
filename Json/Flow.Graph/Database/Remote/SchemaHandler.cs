// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Friflo.Json.Flow.Database.Remote
{
    public class SchemaHandler : IHttpContextHandler
    {
        private const string BasePath = "/schema";
        
        public async Task<bool> HandleContext(HttpListenerContext context, HttpHostDatabase hostDatabase) {
            HttpListenerRequest  req  = context.Request;
            HttpListenerResponse resp = context.Response;
            if (req.HttpMethod == "GET" && req.Url.AbsolutePath.StartsWith(BasePath)) {
                var path = req.Url.AbsolutePath.Substring(BasePath.Length); 
                GetSchemaFile(path, hostDatabase, out string content, out string contentType);
                byte[]  response   = Encoding.UTF8.GetBytes(content);
                HttpHostDatabase.SetResponseHeader(resp, contentType, HttpStatusCode.OK, response.Length);
                await resp.OutputStream.WriteAsync(response, 0, content.Length).ConfigureAwait(false);
                resp.Close();
                return true;
            }
            return false;
        }
        
        private static void GetSchemaFile(string path, HttpHostDatabase hostDatabase, out string content, out string contentType) {
            var schema = hostDatabase.local.schema;
            if (schema == null) {
                content     = "no schema attached to database";
                contentType = "text/plain";
                return;
            }
            content     = "hello schema";
            contentType = "text/plain";
        }
    }
}