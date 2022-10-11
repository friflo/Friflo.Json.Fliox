// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Mapper.Map;

namespace Friflo.Json.Fliox.Mapper
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class ObjectPatcher : IDisposable
    {
        private             ObjectMapper    mapper;
        public  readonly    Differ          differ;
        
        private readonly    StringBuilder   sb = new StringBuilder();
        private readonly    Patcher         patcher;

      
        public ObjectPatcher() {
            patcher     = new Patcher();
            differ      = new Differ();
        }

        public void Dispose() {
            differ.Dispose();
            patcher.Dispose();
        }

        public List<JsonPatch> CreatePatches(DiffNode diff, ObjectMapper mapper) {
            this.mapper = mapper;
            var patches = new List<JsonPatch>();
            if (diff != null)
                TraceDiff(diff, patches);
            this.mapper = null;
            return patches;
        }
        
        public List<JsonPatch> GetPatches<T>(T left, T right, ObjectMapper mapper) {
            var diff = differ.GetDiff(left, right, mapper.writer);
            var patches = CreatePatches(diff, mapper);
            return patches;
        }

        public void ApplyPatches<T>(T root, IList<JsonPatch> patches, ObjectReader reader) {
            var typeCache = reader.TypeCache;
            var rootMapper = (TypeMapper<T>) typeCache.GetTypeMapper(typeof(T));
            var count = patches.Count;
            for (int n = 0; n < count; n++) {
                var patch = patches[n];
                patcher.Patch(rootMapper, root, patch, reader);
            }
        }
        
        public void ApplyDiff<T>(T root, DiffNode diff, ObjectMapper mapper) {
            List<JsonPatch> patches = CreatePatches(diff, mapper);
            ApplyPatches(root, patches, mapper.reader);
        }

        private void TraceDiff(DiffNode diff, List<JsonPatch> patches) {
            switch (diff.DiffType) {
                case DiffType.NotEqual:
                    sb.Clear();
                    diff.AddPath(sb);
                    var json = mapper.writer.WriteVarAsArray(diff.ValueRight);
                    JsonPatch patch = new PatchReplace {
                        path = sb.ToString(),
                        value = new JsonValue(json)
                    };
                    patches.Add(patch);
                    break;
                case DiffType.OnlyLeft:
                    sb.Clear();
                    diff.AddPath(sb);
                    patch = new PatchRemove {
                        path = sb.ToString()
                    };
                    patches.Add(patch);
                    break;
                case DiffType.OnlyRight:
                    sb.Clear();
                    diff.AddPath(sb);
                    json = mapper.writer.WriteVarAsArray(diff.ValueRight);
                    patch = new PatchAdd {
                        path = sb.ToString(),
                        value = new JsonValue(json)
                    };
                    patches.Add(patch);
                    break;
            }
            if (diff.DiffType == DiffType.None) {
                var children = diff.children;
                for (int n = 0; n < children.Count; n++) {
                    var child = children[n];
                    TraceDiff(child, patches);
                }
            }
        }
    }
}