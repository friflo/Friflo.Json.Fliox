// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Transform.Tree;

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
                case JsonEvent.ValueNull:
                    writer.ElementNul   ();
                    break;
                case JsonEvent.ValueBool:
                    writer.ElementBln   (parser.boolValue);
                    break;
                case JsonEvent.ValueNumber:
                    writer.ElementBytes (ref parser.value);
                    break;
                case JsonEvent.ValueString:
                    writer.ElementStr   (parser.value);
                    break;
                case JsonEvent.ObjectStart:
                    writer.ObjectStart  ();
                    parser.NextEvent();
                    TraverseObject(0);  // descend
                    writer.ObjectEnd    ();
                    return;
                case JsonEvent.ArrayStart:
                    writer.ArrayStart   (false);
                    parser.NextEvent();
                    TraverseArray(0);   // descend
                    writer.ArrayEnd     ();
                    return;
                case JsonEvent.ObjectEnd:
                case JsonEvent.ArrayEnd:
                case JsonEvent.EOF:
                default:
                    throw new InvalidOperationException($"unexpected state: {ev}");
            }
            parser.NextEvent();
        }


        private void TraverseArray(int astIndex) {
            while (true) {
                var ev = parser.Event;
                switch (ev) {
                    case JsonEvent.ValueNull:
                        writer.ElementNul   ();
                        break;
                    case JsonEvent.ValueBool:
                        writer.ElementBln   (parser.boolValue);
                        break;
                    case JsonEvent.ValueNumber:
                        writer.ElementBytes (ref parser.value);
                        break;
                    case JsonEvent.ValueString:
                        writer.ElementStr   (parser.value);
                        break;
                    case JsonEvent.ObjectStart:
                        writer.ObjectStart  ();
                        parser.NextEvent();
                        TraverseObject(0);  // descend
                        writer.ObjectEnd    ();
                        break;
                    case JsonEvent.ArrayStart:
                        writer.ArrayStart   (false);
                        parser.NextEvent();
                        TraverseArray(0);   // descend
                        writer.ArrayEnd     ();
                        break;
                    case JsonEvent.ArrayEnd:
                        return;
                    case JsonEvent.ObjectEnd:
                    case JsonEvent.EOF:
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
                    case JsonEvent.ValueNull:
                        writer.MemberNul        (parser.key);
                        break;
                    case JsonEvent.ValueBool:
                        writer.MemberBln        (parser.key, parser.boolValue);
                        break;
                    case JsonEvent.ValueNumber:
                        writer.MemberBytes      (parser.key, ref parser.value);
                        break;
                    case JsonEvent.ValueString:
                        writer.MemberStr        (parser.key, parser.value);
                        break;
                    case JsonEvent.ObjectStart:
                        writer.MemberObjectStart(parser.key);
                        parser.NextEvent();
                        TraverseObject (-1);    // descend
                        writer.ObjectEnd        ();
                        break;
                    case JsonEvent.ArrayStart:
                        writer.MemberArrayStart (parser.key);
                        parser.NextEvent();
                        Start(0);               // descend
                        writer.ArrayEnd         ();
                        break;
                    case JsonEvent.ObjectEnd:
                        return;
                    case JsonEvent.ArrayEnd:
                    case JsonEvent.EOF:
                    default:
                        throw new InvalidOperationException($"unexpected state: {ev}");
                }
                parser.NextEvent();
            }
        }
    }
}