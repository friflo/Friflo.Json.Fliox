// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Mapper.Graph
{
    internal class PathNode {
        internal            SelectQuery                     select;
        internal readonly   SelectorNode                    selectorNode;
        internal readonly   Dictionary<string, PathNode>    children = new Dictionary<string, PathNode>();
        public   override   string                          ToString() => selectorNode.name;

        internal PathNode(SelectorNode selectorNode) {
            this.selectorNode = selectorNode;
        }

        private static void PathNodeToSelectorNode(string path, int start, int end, List<SelectorNode> selectorNodes) {
            int len = end - start;
            var arrayStart = path.IndexOf('[', start);
            var arrayEnd   = path.IndexOf(']', start);
            if (arrayStart != -1 || arrayEnd != -1) {
                if (arrayStart + 2 == arrayEnd) {
                    if (path[arrayStart + 1] != '*')
                        throw new InvalidOperationException($"unsupported array selector: {path.Substring(start, len)}");
                    var token = path.Substring(start, arrayStart - start);
                    selectorNodes.Add(new SelectorNode (token, SelectorType.Member));
                    selectorNodes.Add(new SelectorNode ("[*]", SelectorType.ArrayWildcard));
                    return;
                }
                throw new InvalidOperationException($"Invalid array selector: {path.Substring(start, len)}");
            }
            var memberToken = path.Substring(start, len);
            selectorNodes.Add(new SelectorNode (memberToken, SelectorType.Member));
        }

        private static void PathToSelectorNodes(string path, List<SelectorNode> selectorNodes) {
            selectorNodes.Clear();
            int last = 1;
            int len = path.Length;
            if (len == 0)
                return;
            for (int n = 1; n < len; n++) {
                if (path[n] == '.') {
                    PathNodeToSelectorNode(path, last, n, selectorNodes);
                    last = n + 1;
                }
            }
            PathNodeToSelectorNode(path, last, len, selectorNodes);
        }

        internal static void CreatePathTree(PathNode rootNode, List<SelectQuery> selects, List<SelectorNode> selectorNodes) {
            rootNode.children.Clear();
            var isArrayResult = false;
            var count = selects.Count;
            for (int n = 0; n < count; n++) {
                var select = selects[n];
                PathToSelectorNodes(select.path, selectorNodes);
                PathNode curNode = rootNode;
                for (int i = 0; i < selectorNodes.Count; i++) {
                    var selectorNode = selectorNodes[i];
                    if (!curNode.children.TryGetValue(selectorNode.name, out PathNode childNode)) {
                        childNode = new PathNode(selectorNode);
                        curNode.children.Add(selectorNode.name, childNode);
                    }
                    if (curNode.selectorNode.selectorType == SelectorType.ArrayWildcard)
                        isArrayResult = true;
                    curNode = childNode;
                }
                curNode.select = select;
                if (isArrayResult) {
                    var sb = new StringBuilder();
                    sb.Append('[');
                    curNode.select.arrayResult = sb;
                }
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
        internal    string          path;
        internal    string          jsonResult;
        internal    StringBuilder   arrayResult;
        internal    int             itemCount;
    }
    
    public enum SelectorType
    {
        Root,
        Member,
        ArrayWildcard
    }
    
    internal readonly struct SelectorNode
    {
        internal SelectorNode(string name, SelectorType selectorType) {
            this.name           = name;
            this.selectorType   = selectorType;
        }
        
        internal readonly   string         name;
        internal readonly   SelectorType   selectorType;
    }
}