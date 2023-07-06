// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Hub.Host.Utils
{
    public sealed class SQLConverter : IDisposable
    {
        public              Utf8JsonParser      parser;
        public              Bytes               buffer  = new Bytes(256);
        
        public void Dispose() {
            parser.Dispose();
        }
    }
}