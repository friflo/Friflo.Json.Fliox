// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Transform.Project
{
    public class JsonProjector: IDisposable
    {
        private             Utf8JsonWriter          serializer;
            
        private             Bytes                   targetJson = new Bytes(128);
        private             Utf8JsonParser          parser;
        
        public              string                  ErrorMessage => parser.error.msg.AsString();

        public void Dispose() {
            parser.Dispose();
            targetJson.Dispose();
            serializer.Dispose();
        }

        public JsonValue Project(in SelectionNode node, in JsonValue value) {
            targetJson.Clear();
            targetJson.AppendArray(value);
            parser.InitParser(targetJson);
            parser.NextEvent();
            serializer.InitSerializer();
            serializer.SetPretty(true);

            TraceTree(node);
            if (parser.error.ErrSet)
                return default;

            return new JsonValue(serializer.json.AsArray());
        }
        
        private bool TraceObject(in SelectionNode node) {
            while (Utf8JsonWriter.NextObjectMember(ref parser)) {
                if (!node.FindByBytes(ref parser.key, out var subNode)) {
                    parser.SkipEvent();
                    continue;
                }
                switch (parser.Event) {
                    case JsonEvent.ArrayStart:
                        serializer.MemberArrayStart(in parser.key);
                        TraceArray(subNode);
                        break;
                    case JsonEvent.ObjectStart:
                        serializer.MemberObjectStart(in parser.key);
                        TraceObject(subNode);
                        break;
                    case JsonEvent.ValueString:
                        serializer.MemberStr(in parser.key, in parser.value);
                        break;
                    case JsonEvent.ValueNumber:
                        serializer.MemberBytes(in parser.key, ref parser.value);
                        break;
                    case JsonEvent.ValueBool:
                        serializer.MemberBln(in parser.key, parser.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        serializer.MemberNul(in parser.key);
                        break;
                    case JsonEvent.ObjectEnd:
                    case JsonEvent.ArrayEnd:
                    case JsonEvent.Error:
                    case JsonEvent.EOF:
                        throw new InvalidOperationException("WriteObject() unreachable"); // because of behaviour of ContinueObject()
                }
            }
            serializer.ObjectEnd();
            return true;
        }
        
        private bool TraceArray(in SelectionNode node) {
            while (Utf8JsonWriter.NextArrayElement(ref parser)) {
                switch (parser.Event) {
                    case JsonEvent.ArrayStart:
                        serializer.ArrayStart(true);
                        TraceArray(node);
                        break;
                    case JsonEvent.ObjectStart:
                        serializer.ObjectStart();
                        TraceObject(node);
                        break;
                    case JsonEvent.ValueString:
                        serializer.ElementStr(in parser.value);
                        break;
                    case JsonEvent.ValueNumber:
                        serializer.ElementBytes (ref parser.value);
                        break;
                    case JsonEvent.ValueBool:
                        serializer.ElementBln(parser.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        serializer.ElementNul();
                        break;
                    case JsonEvent.ObjectEnd:
                    case JsonEvent.ArrayEnd:
                    case JsonEvent.Error:
                    case JsonEvent.EOF:
                        throw new InvalidOperationException("TraceArray() unreachable");  // because of behaviour of ContinueArray()
                }
            }
            serializer.ArrayEnd();
            return true;
        }
        
        private bool TraceTree(in SelectionNode node) {
            switch (parser.Event) {
                case JsonEvent.ObjectStart:
                    serializer.ObjectStart();
                    return TraceObject(node);
                case JsonEvent.ArrayStart:
                    serializer.ArrayStart(true);
                    return TraceArray(node);
                case JsonEvent.ValueString:
                    serializer.ElementStr(in parser.value);
                    return true;
                case JsonEvent.ValueNumber:
                    serializer.ElementBytes(ref parser.value);
                    return true;
                case JsonEvent.ValueBool:
                    serializer.ElementBln(parser.boolValue);
                    return true;
                case JsonEvent.ValueNull:
                    serializer.ElementNul();
                    return true;
            }
            return false;
        }
    }
}
