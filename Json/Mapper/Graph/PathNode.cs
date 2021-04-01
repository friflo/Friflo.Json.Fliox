// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Mapper.Graph
{
    internal class PathNode {
        internal            SelectQuery                     select;
        internal            PathType                        pathType;
        private  readonly   SelectorNode                    selectorNode;
        internal readonly   Dictionary<string, PathNode>    children = new Dictionary<string, PathNode>();
        public   override   string                          ToString() => selectorNode.name;

        internal PathNode(SelectorNode selectorNode) {
            this.selectorNode = selectorNode;
        }
        
        private static void PathToSelectorNode(string path, List<SelectorNode> selectorNodes) {
            selectorNodes.Clear();
            int last = 1;
            int len = path.Length;
            if (len == 0)
                return;
            for (int n = 1; n < len; n++) {
                if (path[n] == '/') {
                    var token = path.Substring(last, n - last);
                    var selectorNode = new SelectorNode {
                        name = token
                    };
                    selectorNodes.Add(selectorNode);
                    last = n + 1;
                }
            }
            var lastToken = path.Substring(last, len - last);
            var lastNode = new SelectorNode {
                name = lastToken
            };
            selectorNodes.Add(lastNode);
        }

        internal static void CreatePathTree(PathNode rootNode, List<SelectQuery> selects, List<SelectorNode> selectorNodes) {
            rootNode.children.Clear();
            var count = selects.Count;
            for (int n = 0; n < count; n++) {
                var select = selects[n];
                PathToSelectorNode(select.path, selectorNodes);
                PathNode curNode = rootNode;
                for (int i = 0; i < selectorNodes.Count; i++) {
                    var selectorNode = selectorNodes[i];
                    if (!curNode.children.TryGetValue(selectorNode.name, out PathNode childNode)) {
                        childNode = new PathNode(selectorNode);
                        curNode.children.Add(selectorNode.name, childNode);
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
    
    internal struct SelectorNode
    {
        internal string name;
    }
}