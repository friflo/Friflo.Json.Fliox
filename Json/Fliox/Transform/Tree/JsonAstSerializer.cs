// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Transform.Tree
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class JsonAstSerializer : IDisposable
    {
        private             Utf8JsonParser      parser;
        private             Bytes               json    = new Bytes(128);
        private  readonly   List<JsonAstNode>   nodes   = new List<JsonAstNode>();
        private  readonly   Utf8Buffer          buffer  = new Utf8Buffer();
        
        private             Utf8String?         @null;
        private             Utf8String?         @true;
        private             Utf8String?         @false;


        public JsonAst CreateAst(in JsonValue value) {
            @null   = null;
            @true   = null;
            @false  = null;
            buffer.Clear();
            json.Clear();
            json.AppendArray(value);
            parser.InitParser(json);
            parser.NextEvent();
            
            nodes.Clear();
            Traverse(true);
            return new JsonAst(nodes);
        }
        
        private void Traverse(bool isObject) {
            int         lastIndex   = -1;
            JsonEvent   lastEvent   = default;
            Utf8String  key         = default;
            Utf8String  value       = default;
            bool        isFirst     = true;
            while (true) {
                var index = nodes.Count;
                parser.NextEvent();
                if (!isFirst) {
                    nodes[lastIndex] = new JsonAstNode(lastEvent, key, value, index);    
                }
                isFirst = false;
                var ev  = parser.Event;
                key     = isObject ? buffer.Add(parser.key) : default;
                switch (ev) {
                    case JsonEvent.ObjectStart:
                        nodes.Add(default); // add placeholder
                        value = default;
                        Traverse(true);
                        break;
                    case JsonEvent.ObjectEnd:
                        nodes[lastIndex] = new JsonAstNode(lastEvent, key, value, -1); // last object member
                        return;
                    case JsonEvent.ValueNull: {
                        nodes.Add(default); // add placeholder
                        if (!@null.HasValue)        @null = buffer.Add(parser.value);
                        value = @null.Value;
                        break;
                    }
                    case JsonEvent.ValueBool:
                        nodes.Add(default); // add placeholder
                        if (parser.boolValue) {
                            if (!@true.HasValue)    @true = buffer.Add(parser.value);
                            value = @true.Value;
                            break;
                        }
                        if (!@false.HasValue)       @false = buffer.Add(parser.value);
                        value = @false.Value;
                        break;
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber: {
                        nodes.Add(default); // add placeholder
                        value = buffer.Add(parser.value);
                        break;
                    }
                    case JsonEvent.ArrayStart:
                        nodes.Add(default); // add placeholder
                        value = default;
                        Traverse(false);
                        break;
                    case JsonEvent.ArrayEnd:
                        nodes[lastIndex] = new JsonAstNode(lastEvent, key, value, -1);  // last array item
                        return;
                    default:
                        throw new InvalidOperationException($"unexpected state: {ev}");
                }
                lastIndex = index;
                lastEvent = ev;
            }
        }

        public void Dispose() {
            json.Dispose();
            parser.Dispose();
        }
    }
}