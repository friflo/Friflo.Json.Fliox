// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
        public          byte[]          Response            { get; private set; }
        public          int             Offset              { get; private set; }
        public          int             Length              { get; private set; }
        
        public RequestContext (string path, string  method, Stream body) {
            this.path   = path;
            this.method = method;
            this.body   = body;
        }
        
        public void Write (byte[] value, int offset, int count, string contentType, HttpStatusCode status) {
            ResponseContentType = contentType;
            Status              = status;
            Response            = value;
            Offset              = offset;
            Length              = count;
        }
        
        public void WriteString (string value, string contentType, HttpStatusCode status) {
            var result = new MemoryStream();
            using (var writer = new StreamWriter(result, Encoding.UTF8) { AutoFlush = true }) {
                writer.Write(value);
                writer.Flush();
                ResponseContentType = contentType;
                Status              = status;
                Response            = result.ToArray();
                Offset              = 0;
                Length              = (int)result.Length;
            }
        }
    }
}