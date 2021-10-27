// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Remote
{
    public interface IRequestHandler
    {
        Task<bool> HandleRequest(RequestContext context);
    }
    
    public sealed class RequestContext
    {
        public readonly string          method;
        public readonly string          path;
        public readonly Stream          body;
        
        public          string          ResponseContentType { get; private set; }
        public          int             StatusCode          { get; private set; }
        public          JsonValue       Response            { get; private set; }
        public          int             Offset              { get; private set; }
        
        public RequestContext (string  method,string path, Stream body) {
            this.method = method;
            this.path   = path;
            this.body   = body;
        }
        
        public void Write (JsonValue value, int offset, string contentType, int statusCode) {
            ResponseContentType = contentType;
            StatusCode          = statusCode;
            Response            = value;
            Offset              = offset;
        }
        
        public void WriteString (string value, string contentType, int statusCode) {
            ResponseContentType = contentType;
            StatusCode          = statusCode;
            Response            = new JsonValue(value);
            Offset              = 0;
        }
    }
}