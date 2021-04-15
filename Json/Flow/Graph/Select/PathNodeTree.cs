// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Flow.Graph.Select
{
    internal class PathNodeTree<T>
    {
        internal readonly   PathNode<T>             rootNode        = new PathNode<T>(new SelectorNode("$", SelectorType.Root), null);
        internal readonly   List<PathSelector<T>>   selectors       = new List<PathSelector<T>>();
        private  readonly   List<SelectorNode>      selectorNodes   = new List<SelectorNode>(); // reused buffer

        internal void CreateNodeTree(IList<string> pathList) {
            selectors.Clear();
            rootNode.children.Clear();
            var count = pathList.Count;
            for (int n = 0; n < count; n++) {
                bool isArrayResult = false;
                var path = pathList[n];
                SelectorNode.PathToSelectorNodes(path, selectorNodes);
                PathNode<T> curNode = rootNode;
                for (int i = 0; i < selectorNodes.Count; i++) {
                    var selectorNode = selectorNodes[i];
                    if (!curNode.children.TryGetValue(selectorNode.name, out PathNode<T> childNode)) {
                        childNode = new PathNode<T>(selectorNode, curNode);
                        curNode.children.Add(selectorNode.name, childNode);
                    }
                    var type = selectorNode.selectorType;
                    if (type == SelectorType.ArrayWildcard || type == SelectorType.ArrayGroup) {
                        isArrayResult = true;
                        curNode.wildcardNode = childNode;
                    }
                    curNode = childNode;
                }
                var parentGroup = GetParentGroup(selectorNodes, curNode);
                var selector = new PathSelector<T>(path, curNode, isArrayResult, parentGroup);
                curNode.selectors.Add(selector);
                selectors.Add(selector);
            }
        }

        private PathNode<T> GetParentGroup(List<SelectorNode> selectorNodes, PathNode<T> pathNode) {
            for (int n = selectorNodes.Count - 1; n >= 0; n--) {
                var selectorNode = selectorNodes[n];
                if (selectorNode.selectorType == SelectorType.ArrayGroup)
                    return pathNode;
                pathNode = pathNode.parent;
            }
            return null;
        }
    }
}