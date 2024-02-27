// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Burst;
using static Friflo.Json.Burst.JsonEvent;

namespace Friflo.Json.Fliox.Transform.Tree
{
    /// <summary>
    /// Used to create a tree representation - a <see cref="JsonAst"/> - for a given <see cref="JsonValue"/>
    /// by using <see cref="CreateAst"/>
    /// </summary>
    [CLSCompliant(true)]
    public sealed class JsonAstReader : IDisposable
    {
        private             Utf8JsonParser      parser;
        /// for debugging use <see cref="JsonAstIntern.DebugNodes"/>
        private             JsonAstIntern       ast;
        private  readonly   JsonAst             astApi  = new JsonAst();
        
        private  static readonly    JsonAstSpan     Null;
        private  static readonly    JsonAstSpan     True;
        private  static readonly    JsonAstSpan     False;
        internal static readonly    byte[]          NullTrueFalse; 
        
        public JsonAstReader() {
            ast = new JsonAstIntern(1);
        }
        
        static JsonAstReader() {
            Null            = new JsonAstSpan(1, 4);    // "null"
            True            = new JsonAstSpan(5, 4);    // "true"
            False           = new JsonAstSpan(9, 5);    // "false"
            // ReSharper disable once StringLiteralTypo
            NullTrueFalse   = Encoding.UTF8.GetBytes("~nulltruefalse"); // ~ placeholder for JsonAstSpan.start == 0
        }

        public JsonAst CreateAst(in JsonValue value) {
            parser.InitParser(value);
            ast.Init();
            
            Start();

            astApi.intern = ast;
            parser.NextEvent(); // read EOF
            if (parser.error.ErrSet) {
                astApi.intern.error = parser.error.GetMessage();
            }
            return astApi;
        }
        
        public void Test(in JsonValue value) {
            parser.InitParser(value);
            parser.SkipTree();
        }
        
        private void Start() {
            var ev = parser.NextEvent();
            switch (ev) {
                case ObjectStart: {
                    var child   = parser.NextEvent() != ObjectEnd ? 1 : -1;
                    ast.AddContainerNode(ObjectStart, default, child);
                    TraverseObject();
                    break;
                }
                case ArrayStart: {
                    var child   = parser.NextEvent() != ArrayEnd ? 1 : -1;
                    ast.AddContainerNode(ArrayStart, default, child);
                    TraverseArray();
                    break;
                }
                case ValueNull:
                    ast.AddNode(ValueNull, default, Null);
                    break;
                case ValueBool: {
                    var value   = parser.boolValue ? True : False;
                    ast.AddNode(ValueBool, default, value);
                    break;
                }
                case ValueString:
                case ValueNumber: {
                    var value   = ast.AddSpan(parser.value);
                    ast.AddNode(ev, default, value);
                    break;
                }
                case ArrayEnd:
                case ObjectEnd:
                case EOF:
                default:
                    return;
            }
        }
        
        private void TraverseObject() {
            int prevNode   = -1;
            while (true) {
                var index   = ast.nodesCount;
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
                        TraverseArray();
                        break;
                    }
                    case ArrayEnd:
                    case EOF:
                    default:
                        return;
                }
                parser.NextEvent();
                prevNode = index;
            }
        }
        
        private void TraverseArray() {
            int prevNode   = -1;
            while (true) {
                var index   = ast.nodesCount;
                var ev      = parser.Event;
                switch (ev) {
                    case ObjectStart: {
                        if (prevNode != -1) ast.SetNodeNext(prevNode, index);
                        var child = parser.NextEvent() != ObjectEnd ? index + 1 : -1; 
                        ast.AddContainerNode(ObjectStart, default, child);
                        TraverseObject();
                        break;
                    }
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
                        TraverseArray();
                        break;
                    }
                    case ArrayEnd:
                        return;
                    case ObjectEnd:
                    case EOF:
                    default:
                        return;
                }
                parser.NextEvent();
                prevNode = index;
            }
        }

        public void Dispose() {
            parser.Dispose();
        }
    }
}