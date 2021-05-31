// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Flow.Transform
{
    public class JsonValidator : IDisposable
    {
        private             Bytes           jsonBytes = new Bytes(128);
        private             JsonParser      parser;
        
        public bool ValidJson(string json, out string error) {
            jsonBytes.Clear();
            jsonBytes.AppendString(json);
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