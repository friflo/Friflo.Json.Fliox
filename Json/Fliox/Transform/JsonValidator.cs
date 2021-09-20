// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Transform
{
    public class JsonValidator : IDisposable
    {
        private             Bytes           jsonBytes = new Bytes(128);
        private             JsonParser      parser;
        
        public bool IsValidJson(Utf8Array json, out string error) {
            jsonBytes.Clear();
            jsonBytes.AppendArray(json);
            parser.InitParser(jsonBytes);
            while (true) {
                var ev = parser.NextEvent();
                if (ev == JsonEvent.EOF) {
                    error = null;
                    return true;
                }
                if (ev == JsonEvent.Error) {
                    error = parser.error.msg.ToString();
                    return false;
                }
            }
        }
        
        public void Dispose() {
            jsonBytes.Dispose();
            parser.Dispose();
        }
    }
}