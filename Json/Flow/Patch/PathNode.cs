// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Flow.Patch
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
        internal readonly   Dictionary<string, PathNode<TResult>>   children = new Dictionary<string, PathNode<TResult>>();
        
        public   override   string                                  ToString() => selectorNode.name;

        internal PathNode(SelectorNode selectorNode) {
            this.selectorNode   = selectorNode;
        }
    }

    public static class PathTools
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
        ArrayWildcard
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
                    selectorNodes.Add(new SelectorNode (Wildcard, SelectorType.ArrayWildcard));
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
        internal readonly   bool        isArrayResult;

        public override string ToString() => path;
        
        internal LeafNode(string path, PathNode<T> node, bool isArrayResult) {
            this.path           = path;
            this.node           = node;
            this.isArrayResult  = isArrayResult;
        }
    }

    public class PathNodeTree<T>
    {
        internal readonly   PathNode<T>         rootNode        = new PathNode<T>(new SelectorNode("root", SelectorType.Root));
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
                        childNode = new PathNode<T>(selectorNode);
                        curNode.children.Add(selectorNode.name, childNode);
                    }
                    if (selectorNode.selectorType == SelectorType.ArrayWildcard) {
                        isArrayResult = true;
                        curNode.wildcardNode = childNode;
                    }
                    curNode = childNode;
                }
                var leaf = new LeafNode<T>(path, curNode, isArrayResult);
                leafNodes.Add(leaf);
            }
        }
    }

}