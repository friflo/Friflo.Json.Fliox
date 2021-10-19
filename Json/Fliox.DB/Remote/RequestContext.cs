// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Net;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Remote
{
    public interface IRequestHandler
    {
        Task<bool> HandleRequest(RequestContext context);
    }
    
    public sealed class RequestContext
    {
        public readonly string          path;
        public readonly string          method;
        public readonly Stream          body;
        
        public          string          ResponseContentType { get; private set; }
        public          HttpStatusCode  Status              { get; private set; }
        public          JsonUtf8        Response            { get; private set; }
        public          int             Offset              { get; private set; }
        
        public RequestContext (string path, string  method, Stream body) {
            this.path   = path;
            this.method = method;
            this.body   = body;
        }
        
        public void Write (JsonUtf8 value, int offset, string contentType, HttpStatusCode status) {
            ResponseContentType = contentType;
            Status              = status;
            Response            = value;
            Offset              = offset;
        }
        
        public void WriteString (string value, string contentType, HttpStatusCode status) {
            ResponseContentType = contentType;
            Status              = status;
            Response            = new JsonUtf8(value);
            Offset              = 0;
        }
    }
}