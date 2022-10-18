// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Transform.Tree;
using static Friflo.Json.Burst.JsonEvent;

namespace Friflo.Json.Fliox.Transform.Merge
{
    public class JsonMerger : IDisposable
    {
        private             Utf8JsonParser  parser;
        private             Utf8JsonWriter  writer;
        private             Bytes           json        = new Bytes(128);
        private readonly    JsonAstReader   astReader   = new JsonAstReader();
        private             JsonAst         ast;
        
        public JsonMerger() { }
        
        public void Dispose() {
            astReader.Dispose();
            json.Dispose();
            parser.Dispose();
            writer.Dispose();
        }

        public void Merge (JsonValue value, JsonValue patch) {
            ast = astReader.CreateAst(patch);
            
            json.Clear();
            json.AppendArray(value);
            parser.InitParser(json);
            parser.NextEvent();
            writer.InitSerializer();
            writer.SetPretty(false);
            
            Start(0);
        }
        
        private void Start(int astIndex)
        {
            var ev  = parser.Event;
            switch (ev) {
                case ValueNull:
                    writer.ElementNul   ();
                    break;
                case ValueBool:
                    writer.ElementBln   (parser.boolValue);
                    break;
                case ValueNumber:
                    writer.ElementBytes (ref parser.value);
                    break;
                case ValueString:
                    writer.ElementStr   (parser.value);
                    break;
                case ObjectStart:
                    writer.ObjectStart  ();
                    parser.NextEvent();
                    TraverseObject(0);  // descend
                    writer.ObjectEnd    ();
                    return;
                case ArrayStart:
                    writer.ArrayStart   (false);
                    parser.NextEvent();
                    TraverseArray(0);   // descend
                    writer.ArrayEnd     ();
                    return;
                case ObjectEnd:
                case ArrayEnd:
                case EOF:
                default:
                    throw new InvalidOperationException($"unexpected state: {ev}");
            }
            parser.NextEvent();
        }


        private void TraverseArray(int astIndex) {
            while (true) {
                var ev = parser.Event;
                switch (ev) {
                    case ValueNull:
                        writer.ElementNul   ();
                        break;
                    case ValueBool:
                        writer.ElementBln   (parser.boolValue);
                        break;
                    case ValueNumber:
                        writer.ElementBytes (ref parser.value);
                        break;
                    case ValueString:
                        writer.ElementStr   (parser.value);
                        break;
                    case ObjectStart:
                        writer.ObjectStart  ();
                        parser.NextEvent();
                        TraverseObject(0);  // descend
                        writer.ObjectEnd    ();
                        break;
                    case ArrayStart:
                        writer.ArrayStart   (false);
                        parser.NextEvent();
                        TraverseArray(0);   // descend
                        writer.ArrayEnd     ();
                        break;
                    case ArrayEnd:
                        return;
                    case ObjectEnd:
                    case EOF:
                    default:
                        throw new InvalidOperationException($"unexpected state: {ev}");
                }
                parser.NextEvent();
            }
        }
        
        private void TraverseObject(int astIndex)
        {
            while (true) {
                var ev  = parser.Event;
                switch (ev) {
                    case ValueNull:
                        writer.MemberNul        (parser.key);
                        break;
                    case ValueBool:
                        writer.MemberBln        (parser.key, parser.boolValue);
                        break;
                    case ValueNumber:
                        writer.MemberBytes      (parser.key, ref parser.value);
                        break;
                    case ValueString:
                        writer.MemberStr        (parser.key, parser.value);
                        break;
                    case ObjectStart:
                        writer.MemberObjectStart(parser.key);
                        parser.NextEvent();
                        TraverseObject (-1);    // descend
                        writer.ObjectEnd        ();
                        break;
                    case ArrayStart:
                        writer.MemberArrayStart (parser.key);
                        parser.NextEvent();
                        Start(0);               // descend
                        writer.ArrayEnd         ();
                        break;
                    case ObjectEnd:
                        return;
                    case ArrayEnd:
                    case EOF:
                    default:
                        throw new InvalidOperationException($"unexpected state: {ev}");
                }
                parser.NextEvent();
            }
        }
    }
}