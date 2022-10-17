// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Burst;
using static Friflo.Json.Burst.JsonEvent;

namespace Friflo.Json.Fliox.Transform.Tree
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class JsonAstReader : IDisposable
    {
        private             Utf8JsonParser      parser;
        private             Bytes               json    = new Bytes(128);
        /// for debugging use <see cref="JsonAst.DebugNodes"/>
        private             JsonAst             ast;
        
        private  static readonly    JsonAstSpan     Null;
        private  static readonly    JsonAstSpan     True;
        private  static readonly    JsonAstSpan     False;
        internal static readonly    byte[]          NullTrueFalse; 
        
        public JsonAstReader() {
            ast = new JsonAst(1);
        }
        
        static JsonAstReader() {
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
            if (ev != EOF)    throw new InvalidOperationException($"Expect EOF. was {ev}");
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
                case ValueString:
                case ValueNumber:
                case ValueBool:
                case ValueNull:
                case ArrayStart:
                case ObjectStart:
                    TraverseValue();
                    break;
                default:
                    throw new InvalidOperationException($"unexpected state: {ev}");
            }
        }
        
        private void TraverseObject() {
            int prevNode   = -1;
            while (true) {
                var index   = ast.NodesCount;
                var ev      = parser.Event;
                switch (ev) {
                    case ObjectStart: {
                        if (prevNode != -1) ast.SetNodeNext(prevNode, index);
                        var key     = ast.AddSpan(parser.key);
                        var child   = parser.NextEvent() != ObjectEnd ? index + 1 : -1;
                        ast.AddContainerNode(ObjectStart, key, child);
                        TraverseObject();
                        break;
                    }
                    case ObjectEnd:
                        return;
                    case ValueNull: {
                        if (prevNode != -1) ast.SetNodeNext(prevNode, index);
                        var key     = ast.AddSpan(parser.key);
                        ast.AddNode(ValueNull, key, Null);
                        break;
                    }
                    case ValueBool: {
                        if (prevNode != -1) ast.SetNodeNext(prevNode, index);
                        var key     = ast.AddSpan(parser.key);
                        var value   = parser.boolValue ? True : False;
                        ast.AddNode(ValueBool, key, value);
                        break;
                    }
                    case ValueString:
                    case ValueNumber: {
                        if (prevNode != -1) ast.SetNodeNext(prevNode, index);
                        var key     = ast.AddSpan(parser.key);
                        var value   = ast.AddSpan(parser.value);
                        ast.AddNode(ev, key, value);
                        break;
                    }
                    case ArrayStart: {
                        if (prevNode != -1) ast.SetNodeNext(prevNode, index);
                        var key     = ast.AddSpan(parser.key);
                        var child   = parser.NextEvent() != ArrayEnd ? index + 1 : -1;
                        ast.AddContainerNode(ArrayStart, key, child);
                        TraverseValue();
                        break;
                    }
                    case ArrayEnd:
                        return;
                    case EOF:
                        return;
                    default:
                        throw new InvalidOperationException($"unexpected state: {ev}");
                }
                parser.NextEvent();
                prevNode = index;
            }
        }
        
        private void TraverseValue() {
            int prevNode   = -1;
            while (true) {
                var index   = ast.NodesCount;
                var ev      = parser.Event;
                switch (ev) {
                    case ObjectStart: {
                        if (prevNode != -1) ast.SetNodeNext(prevNode, index);
                        var child = parser.NextEvent() != ObjectEnd ? index + 1 : -1; 
                        ast.AddContainerNode(ObjectStart, default, child);
                        TraverseObject();
                        break;
                    }
                    case ObjectEnd:
                        return;
                    case ValueNull:
                        ast.AddNode(ValueNull, default, Null);
                        break;
                    case ValueBool: {
                        if (prevNode != -1) ast.SetNodeNext(prevNode, index);
                        var value   = parser.boolValue ? True : False;
                        ast.AddNode(ValueBool, default, value);
                        break;
                    }
                    case ValueString:
                    case ValueNumber: {
                        if (prevNode != -1) ast.SetNodeNext(prevNode, index);
                        var value   = ast.AddSpan(parser.value);
                        ast.AddNode(ev, default, value);
                        break;
                    }
                    case ArrayStart: {
                        if (prevNode != -1) ast.SetNodeNext(prevNode, index);
                        var child = parser.NextEvent() != ArrayEnd ? index + 1 : -1;
                        ast.AddContainerNode(ArrayStart, default, child);
                        TraverseValue();
                        break;
                    }
                    case ArrayEnd:
                        return;
                    case EOF:
                        return;
                    default:
                        throw new InvalidOperationException($"unexpected state: {ev}");
                }
                parser.NextEvent();
                prevNode = index;
            }
        }

        public void Dispose() {
            json.Dispose();
            parser.Dispose();
        }
    }
}