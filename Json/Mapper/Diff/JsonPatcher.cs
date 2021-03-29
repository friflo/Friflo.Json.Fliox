// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.Burst;

namespace Friflo.Json.Mapper.Diff
{
    public class JsonPatcher : IDisposable
    {
        private             JsonSerializer  serializer;
        private             Bytes           input = new Bytes(128);
        private             JsonParser      parser;
        private             Bytes           patchInput = new Bytes(128);
        private             JsonParser      patchParser;
        private readonly    List<PatchNode> nodeStack = new List<PatchNode>();
        private readonly    List<string>    pathNodes = new List<string>(); // reused buffer
        private readonly    PatchNode       rootNode = new PatchNode();

        public void Dispose() {
            patchParser.Dispose();
            patchInput.Dispose();
            parser.Dispose();
            input.Dispose();
            serializer.Dispose();
        }
        
        public string ApplyPatches(string root, IList<Patch> patches, bool pretty = false) {
            PatchNode.CreatePatchTree(rootNode, patches, pathNodes);
            nodeStack.Clear();
            nodeStack.Add(rootNode);
            input.Clear();
            input.AppendString(root);
            parser.InitParser(input);
            parser.NextEvent();
            serializer.InitSerializer();
            serializer.SetPretty(pretty);
            
            TraceTree(ref parser);

            rootNode.ClearChildren();
            return serializer.json.ToString();
        }

        private bool TraceObject(ref JsonParser p) {
            while (JsonSerializer.NextObjectMember(ref p)) {
                string key = p.key.ToString();
                var node = nodeStack[nodeStack.Count - 1];
                var debug = true;
                if (debug && node.children.TryGetValue(key, out PatchNode patch)) {
                    switch (patch.patchType) {
                        case PatchType.Replace:
                            patchInput.Clear();
                            patchInput.AppendString(patch.json);
                            patchParser.InitParser(patchInput);
                            patchParser.NextEvent();
                            serializer.WriteMember(ref p.key, ref patchParser);
                            parser.SkipEvent();
                            break;
                        case PatchType.Add:
                            throw new NotImplementedException("PatchType.Add");
                            break;
                        case PatchType.Remove:
                            throw new NotImplementedException("PatchType.Remove");
                            break;
                    }
                    nodeStack.Add(patch);
                } else {
                    switch (p.Event) {
                        case JsonEvent.ArrayStart:
                            serializer.MemberArrayStart(in p.key);
                            TraceArray(ref p);
                            break;
                        case JsonEvent.ObjectStart:
                            serializer.MemberObjectStart(in p.key);
                            TraceObject(ref p);
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
            }

            switch (p.Event) {
                case JsonEvent.ObjectEnd:
                    serializer.ObjectEnd();
                    nodeStack.RemoveAt(nodeStack.Count - 1);
                    break;
                case JsonEvent.Error:
                case JsonEvent.EOF:
                    return false;
            }
            return true;
        }
        
        private bool TraceArray(ref JsonParser p) {
            while (JsonSerializer.NextArrayElement(ref p)) {
                switch (p.Event) {
                    case JsonEvent.ArrayStart:
                        serializer.ArrayStart(true);
                        TraceArray(ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        serializer.ObjectStart();
                        TraceObject(ref p);
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
            }
            switch (p.Event) {
                case JsonEvent.ArrayEnd:
                    serializer.ArrayEnd();
                    break;
                case JsonEvent.Error:
                case JsonEvent.EOF:
                    return false;
            }
            return true;
        }
        
        private bool TraceTree(ref JsonParser p) {
            switch (p.Event) {
                case JsonEvent.ObjectStart:
                    serializer.ObjectStart();
                    return TraceObject(ref p);
                case JsonEvent.ArrayStart:
                    serializer.ArrayStart(true);
                    return TraceArray(ref p);
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