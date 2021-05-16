// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Mapper.Utils;
using Friflo.Json.Flow.Transform.Select;

namespace Friflo.Json.Flow.Mapper.Select
{
    public class MemberSelector : IDisposable
    {
        private readonly    ObjectWriter    writer;
        
        public              TypeCache       TypeCache => writer.TypeCache;
        
        internal MemberSelector(TypeStore typeStore) {
            writer      = new ObjectWriter(typeStore);
        }
        
        public void Dispose() {
            writer.Dispose();
        }

        public void HandleResult(PathNode<ObjectSelectResult> node, object value) {
            if (node.selectors.Count == 0)
                return;
            
            var json = writer.WriteObject(value);
            foreach (var sel in node.selectors) {
                sel.result.json     = json;
                sel.result.value    = value;
            }
        }
    }
}