// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Flow.Schema.Validation
{
    public class JsonValidator : IDisposable
    {
        private Bytes               jsonBytes = new Bytes(128);
        
        public bool Validate (ref JsonParser parser, string json, ValidationType type, out string error) {
            jsonBytes.Clear();
            jsonBytes.AppendString(json);
            parser.InitParser(jsonBytes);
            var ev = parser.NextEvent();
            if (ev != JsonEvent.ObjectStart) {
                error = $"JSON must be an object.";
                return false;
            }
            while (true) {
                ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                        /* if (parser.key.IsEqualBytes(ref idKey) && !parser.value.IsEqualString(id)) {
                            error = $"entity id does not match key. id: {parser.value.ToString()}";
                            return false;
                        } */
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
            jsonBytes.Dispose();
        }
    }
}