// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal class SelectionFilter: IDisposable
    {
        private             Utf8JsonWriter          serializer;
            
        private             Bytes                   targetJson = new Bytes(128);
        private             Utf8JsonParser          targetParser;
        
        public              string                  ErrorMessage => targetParser.error.msg.AsString();

        public void Dispose() {
            targetParser.Dispose();
            targetJson.Dispose();
            serializer.Dispose();
        }

        internal JsonValue Filter(in SelectionNode node, in JsonValue value) {
            return value;
            targetJson.Clear();
            targetJson.AppendArray(value);
            targetParser.InitParser(targetJson);
            targetParser.NextEvent();
            serializer.InitSerializer();
            serializer.SetPretty(true);

            TraceTree(node);
            if (targetParser.error.ErrSet)
                return default;

            return new JsonValue(serializer.json.AsArray());
        }
        
        private bool TraceObject(in SelectionNode node) {
            ref Utf8JsonParser p = ref targetParser;
            while (Utf8JsonWriter.NextObjectMember(ref targetParser)) {
                if (!node.FindByBytes(ref p.key, out var subNode))
                    continue;

                switch (p.Event) {
                    case JsonEvent.ArrayStart:
                        serializer.MemberArrayStart(in p.key);
                        TraceArray(subNode);
                        break;
                    case JsonEvent.ObjectStart:
                        serializer.MemberObjectStart(in p.key);
                        TraceObject(subNode);
                        break;
                    case JsonEvent.ValueString:
                        serializer.MemberStr(in p.key, in p.value);
                        break;
                    case JsonEvent.ValueNumber:
                        serializer.MemberBytes(in p.key, ref p.value);
                        break;
                    case JsonEvent.ValueBool:
                        serializer.MemberBln(in p.key, p.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        serializer.MemberNul(in p.key);
                        break;
                    case JsonEvent.ObjectEnd:
                    case JsonEvent.ArrayEnd:
                    case JsonEvent.Error:
                    case JsonEvent.EOF:
                        throw new InvalidOperationException("WriteObject() unreachable"); // because of behaviour of ContinueObject()
                }
            }
            return true;
        }
        
        private bool TraceArray(in SelectionNode node) {
            ref Utf8JsonParser p = ref targetParser;

            switch (p.Event) {
                case JsonEvent.ArrayStart:
                    serializer.ArrayStart(true);
                    TraceArray(node);
                    break;
                case JsonEvent.ObjectStart:
                    serializer.ObjectStart();
                    TraceObject(node);
                    break;
                case JsonEvent.ValueString:
                    serializer.ElementStr(in p.value);
                    break;
                case JsonEvent.ValueNumber:
                    serializer.ElementBytes (ref p.value);
                    break;
                case JsonEvent.ValueBool:
                    serializer.ElementBln(p.boolValue);
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
            return true;
        }
        
        private bool TraceTree(in SelectionNode node) {
            ref Utf8JsonParser p = ref targetParser;
            switch (p.Event) {
                case JsonEvent.ObjectStart:
                    serializer.ObjectStart();
                    return TraceObject(node);
                case JsonEvent.ArrayStart:
                    serializer.ArrayStart(true);
                    return TraceArray(node);
                case JsonEvent.ValueString:
                    serializer.ElementStr(in p.value);
                    return true;
                case JsonEvent.ValueNumber:
                    serializer.ElementBytes(ref p.value);
                    return true;
                case JsonEvent.ValueBool:
                    serializer.ElementBln(p.boolValue);
                    return true;
                case JsonEvent.ValueNull:
                    serializer.ElementNul();
                    return true;
            }
            return false;
        }
    }
}

#endif