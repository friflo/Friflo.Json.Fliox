// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Remote
{
    public static class HttpHostUtils
    {
        /// <summary>
        /// Returns true if the given <paramref name="path"/> is handled by the <see cref="HttpHost"/>
        /// </summary>
        public static bool GetFlioxRoute(HttpHost httpHost, string path, out string route) {
            var endpoint = httpHost.endpoint;
            if (path.StartsWith(endpoint)) {
                route = path.Substring(endpoint.Length - 1);
                return true;
            }
            route = null;
            return false;
        }
        
        /// <summary>
        /// Perform a redirect from /fliox -> /fliox/ <br/>
        /// Otherwise return a path mapping error. <br/>
        /// E.g. in case ASP.NET maps <c>"/foo/{*path}"</c> to a an <see cref="HttpHost.endpoint"/> <c>"/fliox/"</c> 
        /// </summary>
        public static  RequestContext ExecuteUnknownPath(HttpHost httpHost, string path, string method) {
            var endpoint = httpHost.endpoint;
            if (path == httpHost.endpointRoot && method == "GET") {
                var context = new RequestContext(httpHost, "GET", path, null, null, null, null);
                context.AddHeader("Location", endpoint);
                context.WriteString($"redirect -> {endpoint}", "text/plain", 302);
                context.handled = true;
                return context;
            } else {
                var context = new RequestContext(httpHost, method, path, null, null, null, null);
                var message = $"Expect path matching HttpHost.endpoint: {endpoint}, path: {path}";
                context.WriteError("internal path mapping error", message, 500);
                context.handled = true;
                return context;
            }
        }
        
    }
}