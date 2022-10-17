// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Transform.Tree;

namespace Friflo.Json.Fliox.Transform.Merge
{
    public class JsonMerger : IDisposable
    {
        private             Utf8JsonParser      parser;
        private             Bytes               json        = new Bytes(128);
        private readonly    JsonAstReader       astReader   = new JsonAstReader();
        private             JsonAstIntern       ast;
        
        public JsonMerger() { }
        
        public void Merge (JsonValue value, JsonValue patch) {
            ast = astReader.CreateAst(patch).intern;
            
            json.Clear();
            json.AppendArray(value);
            parser.InitParser(json);
            
            Start();
        }

        public void Dispose() {
            astReader.Dispose();
            json.Dispose();
            parser.Dispose();
        }

        private void Start() {
            var ev = parser.NextEvent();
            switch (ev) {
                case JsonEvent.ValueString:
                case JsonEvent.ValueNumber:
                case JsonEvent.ValueBool:
                case JsonEvent.ValueNull:
                    Traverse(0);
                    break;
                case JsonEvent.ArrayStart:
                    parser.NextEvent();
                    Traverse(0);
                    break;
                case JsonEvent.ObjectStart:
                    parser.NextEvent();
                    Traverse(0);
                    break;
                default:
                    throw new InvalidOperationException($"unexpected state: {ev}");
            }
        }
        
        private void Traverse(int astIndex) {
            while (true) {
                var ev  = parser.Event;
                parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                        Traverse(-1);
                        break;
                    case JsonEvent.ObjectEnd:
                        return;
                    case JsonEvent.ValueNull: {
                        break;
                    }
                    case JsonEvent.ValueBool:
                        break;
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber: {
                        break;
                    }
                    case JsonEvent.ArrayStart:
                        Traverse(-1);
                        break;
                    case JsonEvent.ArrayEnd:
                        return;
                    case JsonEvent.EOF:
                        return;
                    default:
                        throw new InvalidOperationException($"unexpected state: {ev}");
                }
            }
        }
        
    }
}