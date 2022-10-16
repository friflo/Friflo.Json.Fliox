// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
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
        private             JsonAst             ast;
        
        private  static readonly    JsonAstSpan     Null;
        private  static readonly    JsonAstSpan     True;
        private  static readonly    JsonAstSpan     False;
        internal static readonly    byte[]          NullTrueFalse; 
        
        public JsonAstSerializer() {
            ast = new JsonAst(nodes);
        }
        
        static JsonAstSerializer() {
            Null            = new JsonAstSpan(1, 4);    // "null"
            True            = new JsonAstSpan(5, 4);    // "true"
            False           = new JsonAstSpan(9, 5);   // "false"
            // ReSharper disable once StringLiteralTypo
            NullTrueFalse   = Encoding.UTF8.GetBytes("~nulltruefalse"); // ~ placeholder for JsonAstSpan.start == 0
        }

        public JsonAst CreateAst(in JsonValue value) {
            json.Clear();
            json.AppendArray(value);
            parser.InitParser(json);
            ast.Init();
            
            Start();
            
            var ev = parser.Event; 
            if (ev != JsonEvent.EOF)    throw new InvalidOperationException($"Expect EOF. was {ev}");
            return ast;
        }
        
        public JsonAst Test(in JsonValue value) {
            json.Clear();
            json.AppendArray(value);
            parser.InitParser(json);
            parser.SkipTree();
            
            return default;
        }
        
        private void Start() {
            var ev = parser.NextEvent();
            switch (ev) {
                case JsonEvent.ValueString:
                case JsonEvent.ValueNumber:
                case JsonEvent.ValueBool:
                case JsonEvent.ValueNull:
                    Traverse(false);
                    
                    break;
                case JsonEvent.ArrayStart:
                    parser.NextEvent();
                    Traverse(false);
                    
                    parser.NextEvent();
                    break;
                case JsonEvent.ObjectStart:
                    parser.NextEvent();
                    Traverse(true);
                    
                    parser.NextEvent();
                    break;
                default:
                    throw new InvalidOperationException($"unexpected state: {ev}");
            }
        }
        
        private void Traverse(bool isObject) {
            int         lastIndex   = -1;
            JsonEvent   lastEvent   = default;
            JsonAstSpan key         = default;
            JsonAstSpan value       = default;
            while (true) {
                var index   = nodes.Count;
                if (lastIndex != -1) {
                    nodes[lastIndex] = new JsonAstNode(lastEvent, key, value, index); 
                }
                var ev  = parser.Event;
                switch (ev) {
                    case JsonEvent.ObjectStart:
                        nodes.Add(default);     // reserve node
                        key     = isObject ? ast.AddSpan(parser.key) : default;
                        value   = default;      // object has not value
                        parser.NextEvent();
                        Traverse(true);
                        break;
                    case JsonEvent.ObjectEnd:
                        nodes[lastIndex] = new JsonAstNode(lastEvent, key, value, -1); // last object member
                        return;
                    case JsonEvent.ValueNull: {
                        nodes.Add(default);     // reserve node
                        key     = isObject ? ast.AddSpan(parser.key) : default;
                        value   = Null;
                        break;
                    }
                    case JsonEvent.ValueBool:
                        nodes.Add(default);     // reserve node
                        key     = isObject ? ast.AddSpan(parser.key) : default;
                        value   = parser.boolValue ? True : False;
                        break;
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber: {
                        nodes.Add(default);     // reserve node
                        key     = isObject ? ast.AddSpan(parser.key) : default;
                        value   = ast.AddSpan(parser.value);
                        break;
                    }
                    case JsonEvent.ArrayStart:
                        nodes.Add(default);     // reserve node
                        key     = isObject ? ast.AddSpan(parser.key) : default;
                        value   = default;      // array has not value
                        parser.NextEvent();
                        Traverse(false);
                        break;
                    case JsonEvent.ArrayEnd:
                        nodes[lastIndex] = new JsonAstNode(lastEvent, key, value, -1);  // last array item
                        return;
                    case JsonEvent.EOF:
                        return;
                    default:
                        throw new InvalidOperationException($"unexpected state: {ev}");
                }
                parser.NextEvent();
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