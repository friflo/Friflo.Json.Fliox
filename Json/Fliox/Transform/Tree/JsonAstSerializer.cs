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
            Null            = new JsonAstSpan(0, 4);    // "null"
            True            = new JsonAstSpan(4, 8);    // "true"
            False           = new JsonAstSpan(8, 13);   // "false"
            // ReSharper disable once StringLiteralTypo
            NullTrueFalse   = Encoding.UTF8.GetBytes("nulltruefalse");
        }

        public JsonAst CreateAst(in JsonValue value) {
            json.Clear();
            json.AppendArray(value);
            parser.InitParser(json);
            ast.Init();
            
            Start();
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
                    break;
                case JsonEvent.ObjectStart:
                    parser.NextEvent();
                    Traverse(true);
                    break;
                default:
                    throw new InvalidOperationException($"unexpected state: {ev}");
            }
        }
        
        private void Traverse(bool isObject) {
            int         lastIndex   = -1;
            JsonEvent   lastEvent   = default;
            JsonAstSpan key         = new JsonAstSpan(-1);
            JsonAstSpan value       = new JsonAstSpan(-1);
            bool        isFirst     = true;
            while (true) {
                var index   = nodes.Count;
                if (isFirst) {
                    isFirst = false;
                } else {
                    nodes[lastIndex] = new JsonAstNode(lastEvent, key, value, index); 
                }
                var ev  = parser.Event;
                parser.NextEvent();
                key     = isObject ? ast.AddSpan(parser.key) : default;
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
                        value = Null;
                        break;
                    }
                    case JsonEvent.ValueBool:
                        nodes.Add(default); // add placeholder
                        if (parser.boolValue) {
                            value = True;
                            break;
                        }
                        value = False;
                        break;
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber: {
                        nodes.Add(default); // add placeholder
                        value = ast.AddSpan(parser.value);
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
                    case JsonEvent.EOF:
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