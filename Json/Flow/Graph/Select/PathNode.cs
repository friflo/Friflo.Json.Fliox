// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Flow.Graph.Select
{
    /// Each leaf node in a <see cref="PathNode{TResult}"/> hierarchy has <see cref="result"/> not null.
    /// The route from <see cref="PathNodeTree{T}.rootNode"/> to a leaf node represents a given <see cref="string"/> path. 
    /// The root of the hierarchy is <see cref="PathNodeTree{T}.rootNode"/>
    /// The <see cref="result"/> is intended to store the result when reaching as leaf node.
    internal class PathNode<TResult>
    {
        internal            TResult                                 result;
        /// direct access to <see cref="children"/>[*]
        internal            PathNode<TResult>                       wildcardNode;   
        internal readonly   SelectorNode                            selectorNode;
        internal readonly   PathNode<TResult>                       parent;
        internal readonly   Dictionary<string, PathNode<TResult>>   children = new Dictionary<string, PathNode<TResult>>();
        
        public override string ToString() {
            var sb = new StringBuilder();
            AscendToString(sb);
            return sb.ToString();
        }

        private void AscendToString(StringBuilder sb) {
            if (parent != null)
                parent.AscendToString(sb);
            if (selectorNode.selectorType == SelectorType.Member)
                sb.Append('.');
            sb.Append(selectorNode.name);
        }

        internal PathNode(SelectorNode selectorNode, PathNode<TResult> parent) {
            this.selectorNode   = selectorNode;
            this.parent         = parent;
        }
    }

    internal static class PathTools
    {
        public static string PathToPathTokens(string path, List<string> pathTokens) {
            pathTokens.Clear();
            int last = 1;
            int len = path.Length;
            if (len == 0)
                return path;
            for (int n = 1; n < len; n++) {
                if (path[n] == '/') {
                    var token = path.Substring(last, n - last);
                    pathTokens.Add(token);
                    last = n + 1;
                }
            }
            var lastToken = path.Substring(last, len - last);
            pathTokens.Add(lastToken);
            return path;
        }
    }

    public enum SelectorType
    {
        Root,
        Member,
        ArrayWildcard,
        ArrayGroup
    }
    
    internal readonly struct SelectorNode
    {
        internal SelectorNode(string name, SelectorType selectorType) {
            this.name           = name;
            this.selectorType   = selectorType;
        }
        
        internal        readonly    string          name;
        internal        readonly    SelectorType    selectorType;

        private         const       string          Wildcard = "[*]";

        public   override           string          ToString() => name;

        private static void PathNodeToSelectorNode(string path, int start, int end, List<SelectorNode> selectorNodes) {
            int len = end - start;
            var arrayStart = path.IndexOf('[', start);
            var arrayEnd   = path.IndexOf(']', start);
            if (arrayStart != -1 || arrayEnd != -1) {
                if (arrayStart + 2 == arrayEnd) {
                    SelectorType indexType;
                    var indexChar = path[arrayStart + 1];
                    switch (indexChar) {
                        case '*': indexType = SelectorType.ArrayWildcard;   break;
                        case '.': indexType = SelectorType.ArrayGroup;      break;
                        default:
                            throw new InvalidOperationException($"unsupported array selector: {path.Substring(start, len)}");
                    }
                    var token = path.Substring(start, arrayStart - start);
                    selectorNodes.Add(new SelectorNode (token, SelectorType.Member));
                    selectorNodes.Add(new SelectorNode (Wildcard, indexType));
                    return;
                }
                throw new InvalidOperationException($"Invalid array selector: {path.Substring(start, len)}");
            }
            var memberToken = path.Substring(start, len);
            selectorNodes.Add(new SelectorNode (memberToken, SelectorType.Member));
        }

        internal static void PathToSelectorNodes(string path, List<SelectorNode> selectorNodes) {
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
    }

    internal readonly struct LeafNode<T>
    {
        private  readonly   string      path;
        internal readonly   PathNode<T> node;
        private  readonly   bool        isArrayResult;
        private readonly    PathNode<T> parentGroup;

        public override string ToString() => path;
        
        internal LeafNode(string path, PathNode<T> node, bool isArrayResult, PathNode<T> parentGroup) {
            this.path           = path;
            this.node           = node;
            this.isArrayResult  = isArrayResult;
            this.parentGroup    = parentGroup;
        }
    }

    public class PathNodeTree<T>
    {
        internal readonly   PathNode<T>         rootNode        = new PathNode<T>(new SelectorNode("$", SelectorType.Root), null);
        internal readonly   List<LeafNode<T>>   leafNodes       = new List<LeafNode<T>>();
        private  readonly   List<SelectorNode>  selectorNodes   = new List<SelectorNode>(); // reused buffer

        protected void CreateNodeTree(IList<string> pathList) {
            leafNodes.Clear();
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
                var parentGroup = GetParentGroup(curNode);
                var leaf = new LeafNode<T>(path, curNode, isArrayResult, parentGroup);
                leafNodes.Add(leaf);
            }
        }

        private PathNode<T> GetParentGroup(PathNode<T> node) {
            while (node != null) {
                if (node.selectorNode.selectorType == SelectorType.ArrayGroup)
                    return node;
                node = node.parent;
            }
            return null;
        }
    }

}