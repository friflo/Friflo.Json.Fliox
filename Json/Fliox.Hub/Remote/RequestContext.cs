// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Mapper;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote
{
    public interface IRequestHandler
    {
        bool        IsMatch         (RequestContext context);
        Task        HandleRequest   (RequestContext context);
    }
    
    public sealed class RequestContext
    {
        public readonly string                      method;
        public readonly string                      path;
        public readonly string                      query;
        public readonly Stream                      body;
        public readonly IHttpHeaders                headers;
        public readonly IHttpCookies                cookies;
        public readonly bool                        isWebSocket;
                        Dictionary<string, string>  responseHeaders;
        
        public          string                      ResponseContentType { get; private set; }
        public          int                         StatusCode          { get; private set; }
        public          JsonValue                   Response            { get; private set; }
        public          int                         Offset              { get; private set; }
        public          Dictionary<string, string>  ResponseHeaders     => responseHeaders;

        public override string          ToString() => $"{method} {path}{query}";

        public RequestContext (string  method, string path, string query, Stream body, IHttpHeaders headers, IHttpCookies cookies, bool isWebSocket) {
            this.method     = method;
            this.path       = path;
            this.query      = query;
            this.body       = body;
            this.headers    = headers;
            this.cookies    = cookies;
            this.isWebSocket= isWebSocket;
        }
        
        public void Write (JsonValue value, int offset, string contentType, int statusCode) {
            ResponseContentType = contentType;
            StatusCode          = statusCode;
            Response            = value;
            Offset              = offset;
        }
        
        public void WriteString (string value, string contentType) {
            ResponseContentType = contentType;
            StatusCode          = 200;
            Response            = new JsonValue(value);
            Offset              = 0;
        }
        
        public void WriteError (string errorType, string message, int statusCode) {
            var error           = $"{errorType} > {message}";
            ResponseContentType = "text/plain";
            StatusCode          = statusCode;
            Response            = new JsonValue(error);
            Offset              = 0;
        }
        
        public void AddHeader(string key, string value) {
            if (responseHeaders == null) {
                responseHeaders = new Dictionary<string, string>();
            }
            responseHeaders.Add(key, value);
        }
        
        public void SetHeaders(Dictionary<string, string> headers) {
            responseHeaders = headers;
        }
        
        public static bool IsBasePath(string basePath, string path) {
            if (!path.StartsWith(basePath))
                return false;
            if (path.Length == basePath.Length)
                return true;
            return path[basePath.Length] == '/';
        }
    }
    
    public interface IHttpHeaders {
        string this[string key] { get; }
    }
    
    public interface IHttpCookies {
        string this[string key] { get; }
    }
}