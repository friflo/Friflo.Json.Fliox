// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Mapper.Graph
{
    internal class PatchNode {
        internal            PatchType?                      patchType;
        internal            string                          json;
        internal readonly   Dictionary<string, PatchNode>   children = new Dictionary<string, PatchNode>();

        public override     string                          ToString() => patchType != null ? patchType.ToString() : "---";
        

        private void InitPatchNode(Patch patch) {
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
        
        private static void GetPathNodes(Patch patch, List<string> pathNodes) {
            pathNodes.Clear();
            var patchType = patch.PatchType;
            switch (patchType) {
                case PatchType.Replace:
                    var replace = (PatchReplace) patch;
                    Patcher.PathToPathNodes(replace.path, pathNodes);
                    break;
                case PatchType.Add:
                    var add = (PatchAdd) patch;
                    Patcher.PathToPathNodes(add.path, pathNodes);
                    break;
                case PatchType.Remove:
                    var remove = (PatchRemove) patch;
                    Patcher.PathToPathNodes(remove.path, pathNodes);
                    break;
                default:
                    throw new NotImplementedException($"Patch type not supported. Type: {patch.GetType()}");
            }
        }

        internal static void CreatePatchTree(PatchNode rootNode, IList<Patch> patches, List<string> pathNodes) {
            rootNode.children.Clear();
            rootNode.patchType = null;
            var count = patches.Count;
            for (int n = 0; n < count; n++) {
                var patch = patches[n];
                GetPathNodes(patch, pathNodes);
                PatchNode curNode = rootNode;
                PatchNode childNode = rootNode;
                for (int i = 0; i < pathNodes.Count; i++) {
                    var pathNode = pathNodes[i];
                    if (!curNode.children.TryGetValue(pathNode, out childNode)) {
                        childNode = new PatchNode();
                        curNode.children.Add(pathNode, childNode);
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
