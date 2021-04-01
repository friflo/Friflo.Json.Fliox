// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Mapper.Graph
{
    internal class PathNode {
        internal            SelectQuery                     select;
        internal            PathType                        pathType;
        private  readonly   string                          name;
        internal readonly   Dictionary<string, PathNode>    children = new Dictionary<string, PathNode>();
        public   override   string                          ToString() => name;

        internal PathNode(string name) {
            this.name = name;
        }
        

        internal static void CreatePathTree(PathNode rootNode, List<SelectQuery> selects, List<string> pathTokens) {
            rootNode.children.Clear();
            var count = selects.Count;
            for (int n = 0; n < count; n++) {
                var select = selects[n];
                Patcher.PathToPathTokens(select.path, pathTokens);
                PathNode curNode = rootNode;
                for (int i = 0; i < pathTokens.Count; i++) {
                    var token = pathTokens[i];
                    if (!curNode.children.TryGetValue(token, out PathNode childNode)) {
                        childNode = new PathNode(token);
                        curNode.children.Add(token, childNode);
                    }
                    curNode = childNode;
                }
                curNode.select = select;
            }
        }

        internal void ClearChildren() {
            foreach (var child in children) {
                child.Value.ClearChildren();
                child.Value.children.Clear();
            }
        }
    }
    
    internal class SelectQuery {
        internal    string      path;
        internal    string      jsonResult;
    }
    
    public enum PathType
    {
        Node,
        Array
    }
}