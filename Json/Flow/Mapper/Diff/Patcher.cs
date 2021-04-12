// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Graph.Select;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Utils;

namespace Friflo.Json.Flow.Mapper.Diff
{
    public class Patcher : IDisposable
    {
        public  readonly    TypeCache       typeCache;
        private readonly    JsonReader      jsonReader;
        private             PatchType       patchType;
        private             string          json;
        private             int             pathPos;
        private readonly    List<string>    pathNodes = new List<string>();
        private             string          path;
        
        public Patcher(JsonReader jsonReader) {
            this.jsonReader = jsonReader;
            this.typeCache = jsonReader.TypeCache;
        }

        public void Dispose() { }

        public void Patch<T>(TypeMapper<T> mapper, T root, JsonPatch patch) {
            pathPos = 0;
            patchType = patch.PatchType;
            switch (patchType) {
                case PatchType.Replace:
                    var replace = (PatchReplace) patch;
                    json = replace.value.json;
                    path = PathTools.PathToPathTokens(replace.path, pathNodes);
                    if (pathNodes.Count == 0) {
                        jsonReader.ReadTo(json, root);
                        return;
                    }
                    break;
                case PatchType.Add:
                    var add = (PatchAdd) patch;
                    json = add.value.json;
                    path = PathTools.PathToPathTokens(add.path, pathNodes);
                    break;
                case PatchType.Remove:
                    var remove = (PatchRemove) patch;
                    path = PathTools.PathToPathTokens(remove.path, pathNodes);
                    break;
                default:
                    throw new NotImplementedException($"Patch type not supported. Type: {patch.GetType()}");
            }
            mapper.PatchObject(this, root);
        }

        public bool IsMember(string key) {
            return key.Equals(pathNodes[pathPos]);
        }

        public NodeAction DescendMember(TypeMapper typeMapper, object member, out object value) {
            if (++pathPos >= pathNodes.Count) {
                value = jsonReader.ReadObject(json, typeMapper.type);
                switch (patchType) {
                    case PatchType.Replace:
                    case PatchType.Add:
                        return NodeAction.Assign;
                    case PatchType.Remove:
                        return NodeAction.Remove;
                }
                throw new NotImplementedException($"patchType not implemented: {patchType}");
            }
            typeMapper.PatchObject(this, member);
            value = member;
            return NodeAction.Assign;
        }
        
        public string GetMemberKey() {
            var key = pathNodes[pathPos];
            return key;
        }

        public NodeAction DescendElement(TypeMapper elementType, object element, out object value) {
            if (++pathPos >= pathNodes.Count) {
                value = jsonReader.ReadObject(json, elementType.type);
                return NodeAction.Assign;
            }
            elementType.PatchObject(this, element);
            value = element;
            return NodeAction.Assign;
        }

        public int GetElementIndex(int count) {
            var node = pathNodes[pathPos];
            if (!int.TryParse(node, out int index))
                throw new InvalidOperationException($"Incompatible element index type. index: {node} path: {path}");
            if (index >= count)
                throw new InvalidOperationException($"Element index out of range. Count: {count} index: {index} path: {path}");
            return index;
        }
    }

    public enum NodeAction
    {
        Assign,
        Remove,
    }
}
