// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;

namespace Friflo.Json.Fliox.Mapper.Diff
{
    /// <summary>
    /// Create the a JSON value by the given <see cref="DiffNode"/>. <br/>
    /// The JSON result is intended to be merged (assigned) into a given object by using <see cref="ObjectReader.ReadTo{T}(JsonValue,T)"/>
    /// </summary>
    public class JsonDiff : IDisposable
    {
        private     Writer writer;
        
        public JsonDiff(TypeStore typeStore) {
            writer = new Writer(typeStore);
        }
        
        public void Dispose() {
            writer.Dispose();
        }
        
        public JsonValue CreateJsonDiff (DiffNode diffNode) {
            var result = CreateJsonDiffArray(diffNode);
            return new JsonValue(result);
        }
        
        public byte[] CreateJsonDiffArray (DiffNode diffNode) {
            writer.bytes.Clear();
            writer.level            = 0;
            writer.writeNullMembers = true;

            Traverse(ref writer, diffNode);
            return writer.bytes.AsArray();
        }
        
        private static void Traverse(ref Writer writer, DiffNode diffNode) {
            bool firstMember = true;
            foreach (var child in diffNode.children) {
                // Traverse(ref writer, child);
                var key = child.NodeKey; 
                if (key is PropField field) {
                    writer.WriteFieldKey (field, ref firstMember);
                    child.NodeMapper.WriteVar(ref writer, child.ValueRight);
                }
            }
            writer.WriteObjectEnd(firstMember);
        }
    }
}