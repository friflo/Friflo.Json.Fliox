// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Transform.Project
{
    public sealed class JsonProjector: IDisposable
    {
        private             Utf8JsonWriter          serializer;
        private             Utf8JsonParser          parser;
        private             Bytes                   valueBuf    = new Bytes(1);
        private             Bytes                   __typename;
        
        public              string                  ErrorMessage => parser.error.msg.AsString();
        
        public JsonProjector() {
            __typename   = new Bytes("__typename");
        }

        public void Dispose() {
            __typename.Dispose();
            valueBuf.Dispose();
            parser.Dispose();
            serializer.Dispose();
        }

        public JsonValue Project(in SelectionNode node, in JsonValue value) {
            parser.InitParser(value);
            parser.NextEvent();
            serializer.InitSerializer();
            serializer.SetPretty(true);

            TraceTree(node);
            if (parser.error.ErrSet)
                return default;

            return new JsonValue(serializer.json.AsArray());
        }
        
        private bool TraceObject(in SelectionNode node) {
            if (!node.HasNodes) {
                return serializer.WriteObject(ref parser);
            }
            bool            setUnionType    = (node.emitTypeName && node.unions != null) || node.fragments != null;
            Utf8String      unionType       = default;
            SelectionNode[] fragmentNodes   = null;
            while (Utf8JsonWriter.NextObjectMember(ref parser)) {
                // Expect discriminator as first property
                if (setUnionType && parser.Event == JsonEvent.ValueString) {
                    unionType       = node.FindUnionType(parser.value);
                    fragmentNodes   = node.FindFragmentNodes(unionType);
                }
                setUnionType = false;
                if (!node.FindField(parser.key, out var subNode)) {
                    if (fragmentNodes == null) {
                        parser.SkipEvent();
                        continue;
                    }
                    if (!SelectionNode.FindFragment(fragmentNodes, parser.key, out subNode)) {
                        parser.SkipEvent();
                        continue;
                    }
                }
                switch (parser.Event) {
                    case JsonEvent.ArrayStart:
                        serializer.MemberArrayStart(parser.key.AsSpan());
                        TraceArray(subNode);
                        break;
                    case JsonEvent.ObjectStart:
                        serializer.MemberObjectStart(parser.key.AsSpan());
                        TraceObject(subNode);
                        break;
                    case JsonEvent.ValueString:
                        serializer.MemberStr(parser.key.AsSpan(), parser.value.AsSpan());
                        break;
                    case JsonEvent.ValueNumber:
                        serializer.MemberBytes(parser.key.AsSpan(), parser.value);
                        break;
                    case JsonEvent.ValueBool:
                        serializer.MemberBln(parser.key.AsSpan(), parser.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        serializer.MemberNul(parser.key.AsSpan());
                        break;
                    case JsonEvent.ObjectEnd:
                    case JsonEvent.ArrayEnd:
                    case JsonEvent.Error:
                    case JsonEvent.EOF:
                        throw new InvalidOperationException("WriteObject() unreachable"); // because of behaviour of ContinueObject()
                }
            }
            if (node.emitTypeName) {
                if (node.unions == null) { 
                    node.typeName.CopyTo(ref valueBuf);
                } else {
                    unionType.CopyTo    (ref valueBuf);
                }
                serializer.MemberStr(__typename.AsSpan(), valueBuf.AsSpan());
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
                        serializer.ElementStr(parser.value.AsSpan());
                        break;
                    case JsonEvent.ValueNumber:
                        serializer.ElementBytes (parser.value);
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
                    serializer.ElementStr(parser.value.AsSpan());
                    return true;
                case JsonEvent.ValueNumber:
                    serializer.ElementBytes(parser.value);
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
