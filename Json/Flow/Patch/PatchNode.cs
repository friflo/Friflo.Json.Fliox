// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Select;

namespace Friflo.Json.Flow.Patch
{
    internal class PatchNode {
        internal            PatchType?                      patchType;
        internal            string                          json;
        internal readonly   Dictionary<string, PatchNode>   children = new Dictionary<string, PatchNode>();

        public override     string                          ToString() => patchType != null ? patchType.ToString() : "---";
        

        private void InitPatchNode(JsonPatch patch) {
            patchType = patch.PatchType;
            switch (patchType) {
                case PatchType.Replace:
                    var replace = (PatchReplace) patch;
                    json = replace.value.json;
                    break;
                case PatchType.Add:
                    var add = (PatchAdd) patch;
                    json = add.value.json;
                    break;
                case PatchType.Remove:
                    break;
                default:
                    throw new NotImplementedException($"Patch type not supported. Type: {patch.GetType()}");
            }
        }
        
        private static void GetPathNodes(JsonPatch patch, List<string> pathTokens) {
            pathTokens.Clear();
            var patchType = patch.PatchType;
            switch (patchType) {
                case PatchType.Replace:
                    var replace = (PatchReplace) patch;
                    PathTools.PathToPathTokens(replace.path, pathTokens);
                    break;
                case PatchType.Add:
                    var add = (PatchAdd) patch;
                    PathTools.PathToPathTokens(add.path, pathTokens);
                    break;
                case PatchType.Remove:
                    var remove = (PatchRemove) patch;
                    PathTools.PathToPathTokens(remove.path, pathTokens);
                    break;
                default:
                    throw new NotImplementedException($"Patch type not supported. Type: {patch.GetType()}");
            }
        }

        internal static void CreatePatchTree(PatchNode rootNode, IList<JsonPatch> patches, List<string> pathTokens) {
            rootNode.children.Clear();
            rootNode.patchType = null;
            var count = patches.Count;
            for (int n = 0; n < count; n++) {
                var patch = patches[n];
                GetPathNodes(patch, pathTokens);
                PatchNode curNode = rootNode;
                PatchNode childNode = rootNode;
                for (int i = 0; i < pathTokens.Count; i++) {
                    var token = pathTokens[i];
                    if (!curNode.children.TryGetValue(token, out childNode)) {
                        childNode = new PatchNode();
                        curNode.children.Add(token, childNode);
                    }
                    curNode = childNode;
                }
                childNode.InitPatchNode(patch);
            }
        }

        internal void ClearChildren() {
            foreach (var child in children) {
                child.Value.ClearChildren();
                child.Value.children.Clear();
            }
        }
    }
}
