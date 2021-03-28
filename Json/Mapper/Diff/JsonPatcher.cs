// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.Burst;

namespace Friflo.Json.Mapper.Diff
{
    internal struct PatchItem {
        private             PatchType       patchType;
        private             string          json;
        private             int             pathPos;
        private readonly    List<string>    pathNodes;
        private             string          path;
        
        public void InitPatch(Patch patch) {
            pathPos = 0;
            patchType = patch.PatchType;
            switch (patchType) {
                case PatchType.Replace:
                    var replace = (PatchReplace) patch;
                    json = replace.value.json;
                    path = Patcher.PathToPathNodes(replace.path, pathNodes);
                    break;
                case PatchType.Add:
                    var add = (PatchAdd) patch;
                    json = add.value.json;
                    path = Patcher.PathToPathNodes(add.path, pathNodes);
                    break;
                case PatchType.Remove:
                    var remove = (PatchRemove) patch;
                    path = Patcher.PathToPathNodes(remove.path, pathNodes);
                    break;
                default:
                    throw new NotImplementedException($"Patch type not supported. Type: {patch.GetType()}");
            }
        }
    }
    
    public class JsonPatcher : IDisposable
    {
        private             JsonSerializer  serializer;
        private             JsonParser      parser;
        private             Bytes           input = new Bytes(128);
        private readonly    List<PatchItem> patches = new List<PatchItem>();

        public void Dispose() {
            serializer.Dispose();
            parser.Dispose();
        }
        
        public string ApplyPatches(string root, IList<Patch> patches) {
            patches.Clear();
            var count = patches.Count;
            for (int n = 0; n < count; n++) {
                var patch = patches[n];
                var pathItem = new PatchItem();
                pathItem.InitPatch(patch);
            }
            input.Clear();
            input.AppendString(root);
            parser.InitParser(input);
            serializer.InitSerializer();
            TraceTree(ref parser);



            return root;
        }
        
        public bool TraceObject(ref JsonParser p) {
            while (JsonSerializer.NextObjectMember(ref p)) {
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

            switch (p.Event) {
                case JsonEvent.ObjectEnd:
                    serializer.ObjectEnd();
                    break;
                case JsonEvent.Error:
                case JsonEvent.EOF:
                    return false;
            }
            return true;
        }
        
        public bool TraceArray(ref JsonParser p) {
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
                    serializer.MemberArrayStart(in p.key);
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