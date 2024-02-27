// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Friflo.Json.Burst;
using static Friflo.Json.Burst.JsonEvent;

namespace Friflo.Json.Fliox.Transform.Tree
{
    /// <summary>
    /// Used to create a <see cref="JsonValue"/> from the given <see cref="JsonAst"/>
    /// </summary>
    [CLSCompliant(true)]
    public sealed class JsonAstWriter : IDisposable
    {
        private     Utf8JsonWriter  astWriter;
        private     Bytes           key;
        private     Bytes           value;
        private     JsonAst         ast;
        public      bool            WriteNullMembers { get; set; }
        
        public void Dispose() {
            astWriter.Dispose();
        }
        
        public JsonValue WriteAst(JsonAst ast) {
            WriteAstInternal(ast, ref astWriter);
            return new JsonValue(astWriter.json.AsArray());
        }
        
        public Bytes WriteAstBytes(JsonAst ast) {
            WriteAstInternal(ast, ref astWriter);
            return astWriter.json;
        }
        
        internal void Init(JsonAst ast) {
            this.ast        = ast;
            var buffer      = ast.intern.Buf; 
            key.  buffer    = buffer;
            value.buffer    = buffer;
        }

        /// <summary>Ensure <see cref="Bytes.buffer"/> is not modified at <see cref="Bytes.AppendBytes"/></summary>
        [Conditional("DEBUG")]
        internal void AssertBuffers() {
            var buffer          = ast.intern.Buf;
            if (key.  buffer  != buffer)  throw new InvalidOperationException("key buffer modified");
            if (value.buffer  != buffer)  throw new InvalidOperationException("value buffer modified");
        }
        
        private void WriteAstInternal(JsonAst ast, ref Utf8JsonWriter writer) {
            Init(ast);
            writer.InitSerializer();
            var root = ast.intern.nodes[0];
            
            WriteValue(root, ref writer);

            AssertBuffers();
        }
        
        internal void WriteRootValue(ref Utf8JsonWriter writer) {
            var root = ast.intern.nodes[0];
            WriteValue(root, ref writer);
        }
        
        private void WriteValue(in JsonAstNode node, ref Utf8JsonWriter writer) {
            var ev = node.type;
            switch (ev) {
                case  ObjectStart:
                    if (node.child != -1) {
                        writer.ObjectStart();
                        WriteObject(node.child, ref writer);
                        writer.ObjectEnd();
                    }
                    break;
                case  ArrayStart:
                    if (node.child != -1) {
                        writer.ArrayStart(false);
                        WriteArray(node.child, ref writer);
                        writer.ArrayEnd();
                    }
                    break;
                case ValueNull:
                    writer.ElementNul();
                    break;
                case ValueBool:
                case ValueNumber:
                    value. Set(node.value);
                    writer.ElementBytes(value);
                    break;
                case ValueString:
                    value. Set(node.value);
                    writer.ElementStr(value.AsSpan());
                    break;
            }
        }
        
        private void WriteArray(int index, ref Utf8JsonWriter writer) {
            while (index != - 1) {
                var node = ast.intern.nodes[index];
                WriteValue(node, ref writer);
                index = node.Next;
            }
        }
        
        private void WriteObject(int index, ref Utf8JsonWriter writer) {
            while (index != - 1) {
                index = WriteObjectMember(index, ref writer);
            }
        }
        
        internal int WriteObjectMember(int index, ref Utf8JsonWriter writer)
        {
            var node    = ast.intern.nodes[index];
            var ev      = node.type;
            switch (ev) {
                case  ObjectStart:
                    if (node.child != -1) {
                        key.   Set(node.key);
                        writer.MemberObjectStart(key.AsSpan());
                        WriteObject(node.child, ref writer);
                        writer.ObjectEnd();
                    }
                    break;
                case  ArrayStart:
                    if (node.child != -1) {
                        key.   Set(node.key);
                        writer.MemberArrayStart(key.AsSpan());
                        WriteArray(node.child, ref writer);
                        writer.ArrayEnd();
                    }
                    break;
                case ValueNull:
                    if (WriteNullMembers) { 
                        key.   Set(node.key);
                        writer.MemberNul(key.AsSpan());
                    }
                    break;
                case ValueBool:
                case ValueNumber:
                    key.   Set(node.key);
                    value. Set(node.value);
                    writer.MemberBytes(key.AsSpan(), value);
                    break;
                case ValueString:
                    key.   Set(node.key);
                    value. Set(node.value);
                    writer.MemberStr(key.AsSpan(), value.AsSpan());
                    break;
            }
            return node.next;
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