// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Mapper.Graph
{
    internal class PathNode {
        internal            string                          jsonResult;
        internal            PathType                        pathType;
        internal readonly   Dictionary<string, PathNode>    children = new Dictionary<string, PathNode>();
        public   override   string                          ToString() => string.Join(", ", children.Keys);
        

        internal static void CreatePathTree(PathNode rootNode, List<SelectQuery> selects, List<string> pathNodeBuffer) {
            rootNode.children.Clear();
            var count = selects.Count;
            for (int n = 0; n < count; n++) {
                var select = selects[n];
                Patcher.PathToPathNodes(select.path, pathNodeBuffer);
                PathNode curNode = rootNode;
                for (int i = 0; i < pathNodeBuffer.Count; i++) {
                    var pathNode = pathNodeBuffer[i];
                    if (!curNode.children.TryGetValue(pathNode, out PathNode childNode)) {
                        childNode = new PathNode();
                        curNode.children.Add(pathNode, childNode);
                    }
                    curNode = childNode;
                }
                select.node = curNode;
            }
        }

        internal void ClearChildren() {
            foreach (var child in children) {
                child.Value.ClearChildren();
                child.Value.children.Clear();
            }
        }
    }
    
    internal struct SelectQuery {
        internal    string      path;
        internal    PathNode    node;
    }
    
    public enum PathType
    {
        Node,
        Array
    }
}