// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Transform.Tree
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class JsonAstWriter : IDisposable
    {
        private     Utf8JsonWriter  writer;
        private     Bytes           key;
        private     Bytes           value;
        private     JsonAstIntern   ast;
        
        public void Dispose() {
            writer.Dispose();
        }
        
        public JsonValue WriteAst(JsonAst ast) {
            this.ast            = ast.intern; 
            key.  buffer.array  = ast.intern.Buf;
            value.buffer.array  = ast.intern.Buf;
            writer.InitSerializer();
            
            WriteValue(0);
            
            return new JsonValue(writer.json.AsArray());
        }
        
        private void WriteValue(int index) {
            var nodes = ast.nodes;
            while (index != - 1) {
                var node = nodes[index];
                var ev = node.type;
                switch (ev) {
                    case  JsonEvent.ObjectStart:
                        if (node.child != -1) {
                            writer.ObjectStart();
                            WriteObject(node.child);
                            writer.ObjectEnd();
                        }
                        break;
                    case  JsonEvent.ArrayStart:
                        if (node.child != -1) {
                            writer.ArrayStart(false);
                            WriteValue(node.child);
                            writer.ArrayEnd();
                        }
                        break;
                    case JsonEvent.ValueNull:
                        writer.ElementNul();
                        break;
                    case JsonEvent.ValueBool:
                    case JsonEvent.ValueNumber:
                        value.SetDim(node.value.start, node.value.start + node.value.len);
                        writer.ElementBytes(ref value);
                        break;
                    case JsonEvent.ValueString:
                        value.SetDim(node.value.start, node.value.start + node.value.len);
                        writer.ElementStr(value);
                        break;
                }
                index = node.Next;
            }
        }
        
        private void WriteObject(int index) {
            var nodes = ast.nodes;
            while (index != - 1) {
                var node = nodes[index];
                var ev = node.type;
                switch (ev) {
                    case  JsonEvent.ObjectStart:
                        if (node.child != -1) {
                            key.SetDim(node.key.start,    node.key.start   + node.key.len);
                            writer.MemberObjectStart(key);
                            WriteObject(node.child);
                            writer.ObjectEnd();
                        }
                        break;
                    case  JsonEvent.ArrayStart:
                        if (node.child != -1) {
                            key.SetDim(node.key.start,    node.key.start   + node.key.len);
                            writer.MemberArrayStart(key);
                            WriteValue(node.child);
                            writer.ArrayEnd();
                        }
                        break;
                    case JsonEvent.ValueNull:
                        key.   SetDim(node.key.start,    node.key.start   + node.key.len);
                        writer.MemberNul(key);
                        break;
                    case JsonEvent.ValueBool:
                    case JsonEvent.ValueNumber:
                        key.   SetDim(node.key.start,    node.key.start   + node.key.len);
                        value. SetDim(node.value.start,  node.value.start + node.value.len);
                        writer.MemberBytes(key, ref value);
                        break;
                    case JsonEvent.ValueString:
                        key.   SetDim(node.key.start,    node.key.start   + node.key.len);
                        value. SetDim(node.value.start,  node.value.start + node.value.len);
                        writer.MemberStr(key, value);
                        break;
                }
                index = node.Next;
            }
        }
    }
}