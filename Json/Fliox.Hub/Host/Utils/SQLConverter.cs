// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Hub.Host.Utils
{
    public sealed class SQLConverter : IDisposable
    {
        public      Utf8JsonParser  parser;
        public      Bytes           buffer      = new Bytes(256);
        private     char[]          charBuffer  = new char[32];
        
        public int GetChars(in Bytes bytes, out char[] chars) {
            var max = Encoding.UTF8.GetMaxCharCount(bytes.Len);
            if (max > charBuffer.Length) {
                charBuffer = new char[max];
            }
            chars = charBuffer;
            return Encoding.UTF8.GetChars(bytes.buffer, bytes.start, bytes.end - bytes.start, charBuffer, 0);
        }
        
        public void Dispose() {
            parser.Dispose();
        }
    }
}