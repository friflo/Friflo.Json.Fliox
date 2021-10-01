// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Text;

namespace Friflo.Json.Fliox.DB.Remote
{
    public class RequestContext
    {
        public readonly Uri     url;
        public readonly string  method;
        
        public          string          ResponseContentType { get; private set; }
        public          HttpStatusCode  Status              { get; private set; }
        public          byte[]          Response            { get; private set; }
        public          int             Offset              { get; private set; }
        public          int             Length               { get; private set; }
        
        public RequestContext (Uri url, string  method) {
            this.url    = url;
            this.method = method;
        }
        
        public void Write (byte[] value, int offset, int count, string contentType, HttpStatusCode status) {
            ResponseContentType = contentType;
            this.Status         = status;
            Response            = value;
            this.Offset         = offset;
            this.Length          = count;
        }
        
        public void WriteString (string value, string contentType, HttpStatusCode status) {
            var result = new MemoryStream();
            using (var writer = new StreamWriter(result, Encoding.UTF8) { AutoFlush = true }) {
                writer.Write(value);
                writer.Flush();
                ResponseContentType = contentType;
                this.Status         = status;
                Response            = result.ToArray();
                Offset              = 0;
                Length               = (int)result.Length;
            }
        }
    }
}