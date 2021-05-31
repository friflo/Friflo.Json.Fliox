// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Flow.Database
{
    public class EntityValidator : IDisposable
    {
        private             Bytes           jsonBytes = new Bytes(128);
        private             JsonParser      parser;
        private             Bytes           idKey = new Bytes("id");
        
        public bool IsValidEntity(string json, string id, out string error) {
            jsonBytes.Clear();
            jsonBytes.AppendString(json);
            parser.InitParser(jsonBytes);
            var ev = parser.NextEvent();
            if (ev != JsonEvent.ObjectStart) {
                error = $"entity value must be an object.";
                return false;
            }
            while (true) {
                ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                        if (parser.key.IsEqualBytes(ref idKey) && !parser.value.IsEqualString(id)) {
                            error = $"Invalid entity id in JSON value. Expect: {id}, was: {parser.value.ToString()}";
                            return false;
                        }
                        break;
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ValueNull:
                        break;
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        parser.SkipTree();
                        if (parser.error.ErrSet) {
                            error = parser.error.msg.ToString();
                            return false;
                        }
                        break;
                    case JsonEvent.Error:
                        error = parser.error.msg.ToString();
                        return false;
                    case JsonEvent.ObjectEnd:
                        ev = parser.NextEvent();
                        if (ev == JsonEvent.EOF) {
                            error = null;
                            return true;
                        }
                        error = "Expected EOF in JSON value";
                        return false;
                    case JsonEvent.ArrayEnd:
                        throw new InvalidOperationException($"unexpected event: {ev}");
                }
            }
        }
        
        public void Dispose() {
            idKey.Dispose();
            jsonBytes.Dispose();
            parser.Dispose();
        }
    }
}