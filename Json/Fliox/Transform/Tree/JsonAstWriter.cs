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
            WriteAstBytes(ast);
            return new JsonValue(writer.json.AsArray());
        }
        
        public Bytes WriteAstBytes(JsonAst ast) {
            this.ast            = ast.intern;
            var buffer          = ast.intern.Buf; 
            key.  buffer.array  = buffer;
            value.buffer.array  = buffer;
            writer.InitSerializer();
            
            WriteValue(0);
            
            return writer.json;
        }
        
        private void WriteValue(int index) {
            while (index != - 1)
            {
                var node = ast.nodes[index];
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
                        value. Set(node.value);
                        writer.ElementBytes(ref value);
                        break;
                    case JsonEvent.ValueString:
                        value. Set(node.value);
                        writer.ElementStr(value);
                        break;
                }
                index = node.Next;
            }
        }
        
        private void WriteObject(int index) {
            while (index != - 1)
            {
                var node = ast.nodes[index];
                var ev = node.type;
                switch (ev) {
                    case  JsonEvent.ObjectStart:
                        if (node.child != -1) {
                            key.   Set(node.key);
                            writer.MemberObjectStart(key);
                            WriteObject(node.child);
                            writer.ObjectEnd();
                        }
                        break;
                    case  JsonEvent.ArrayStart:
                        if (node.child != -1) {
                            key.   Set(node.key);
                            writer.MemberArrayStart(key);
                            WriteValue(node.child);
                            writer.ArrayEnd();
                        }
                        break;
                    case JsonEvent.ValueNull:
                        key.   Set(node.key);
                        writer.MemberNul(key);
                        break;
                    case JsonEvent.ValueBool:
                    case JsonEvent.ValueNumber:
                        key.   Set(node.key);
                        value. Set(node.value);
                        writer.MemberBytes(key, ref value);
                        break;
                    case JsonEvent.ValueString:
                        key.   Set(node.key);
                        value. Set(node.value);
                        writer.MemberStr(key, value);
                        break;
                }
                index = node.Next;
            }
        }
    }
    
    internal static class JsonAstWriterExtensions
    {
        internal static  void Set(this ref Bytes bytes, in JsonAstSpan span) {
            bytes.start = span.start;
            bytes.end   = span.start + span.len;
        }
    }
}