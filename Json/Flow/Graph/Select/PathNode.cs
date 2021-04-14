// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Flow.Graph.Select
{
    /// <see cref="PathNode{TResult}"/>'s build a tree of nodes starting from <see cref="PathNodeTree{T}.rootNode"/>.
    /// The tree is build based on a given list of <see cref="string"/> paths.
    /// 
    /// The route from <see cref="PathNodeTree{T}.rootNode"/> to a leaf node represents a given <see cref="string"/> path.
    /// For each path a <see cref="PathSelector{T}"/> is created. The <see cref="PathSelector{T}"/> is then added
    /// to <see cref="selectors"/>.
    /// 
    /// The root of the hierarchy is <see cref="PathNodeTree{T}.rootNode"/>
    /// A <see cref="PathSelector{T}"/> is intended to store the result when reaching a node having <see cref="selectors"/>.
    internal class PathNode<TResult>
    {
        /// direct access to <see cref="children"/>[*]
        internal            PathNode<TResult>                       wildcardNode;
        internal            int                                     arrayIndex;
        
        internal readonly   List<PathSelector<TResult>>             selectors = new List<PathSelector<TResult>>();
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
    
    internal enum SelectorType
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
                        case '@': indexType = SelectorType.ArrayGroup;      break;
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
}
