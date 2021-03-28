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
            return root;
        }
    }
}