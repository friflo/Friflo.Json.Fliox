// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map
{
    public class JsonPatch : IDisposable
    {
        private             List<Patch>     patches;
        private readonly    StringBuilder   sb = new StringBuilder();
        private readonly    JsonMapper      mapper;
        private readonly    TypeCache       typeCache;
        private readonly    Patcher         patcher;

        public JsonPatch(TypeStore typeStore) {
            mapper  = new JsonMapper(typeStore);
            patcher = new Patcher();
            typeCache = mapper.reader.TypeCache;
        }

        public void Dispose() {
            mapper.Dispose();
        }

        public List<Patch> CreatePatches(Diff diff) {
            patches = new List<Patch>();
            TraceDiff(diff);
            return patches;
        }

        public void ApplyPatches<T>(T value, IEnumerable<Patch> patches) {
            var rootMapper = (TypeMapper<T>) typeCache.GetTypeMapper(value.GetType());
            foreach (var patch in patches) { 
                patcher.Patch(rootMapper, value, patch);
            }
        }

        private void TraceDiff(Diff diff) {
            if (diff.diffType == DiffType.Modified) {
                sb.Clear();
                diff.AddPath(sb);
                var json = mapper.WriteObject(diff.right);
                var value = new PatchValue {
                    json        = json
                };
                var replace = new PatchReplace {
                    path  = sb.ToString(),
                    value = value
                };
                patches.Add(replace);
            }
            var children = diff.children;
            if (children != null) {
                for (int n = 0; n < children.Count; n++) {
                    var child = children[n];
                    TraceDiff(child);
                }
            }
        }
    }
}