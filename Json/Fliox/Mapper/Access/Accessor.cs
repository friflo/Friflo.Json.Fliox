// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper.Utils;
using Friflo.Json.Fliox.Transform.Select;

namespace Friflo.Json.Fliox.Mapper.Access
{
    public class Accessor : IDisposable
    {
        private readonly    ObjectWriter    writer;
        
        public              TypeCache       TypeCache => writer.TypeCache;
        
        internal Accessor(TypeStore typeStore) {
            writer      = new ObjectWriter(typeStore);
        }
        
        internal Accessor(ObjectWriter writer) {
            this.writer = writer;
        }
        
        public void Dispose() {
            writer.Dispose();
        }

        public void HandleResult(PathNode<MemberValue> node, object value) {
            if (node.selectors.Count == 0)
                return;
            
            var json = writer.WriteObject(value);
            foreach (var sel in node.selectors) {
                sel.result.Found    = true;
                sel.result.json     = json;
                sel.result.value    = value;
            }
        }
    }
}