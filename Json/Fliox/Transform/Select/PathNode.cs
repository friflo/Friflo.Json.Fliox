// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Transform.Select
{
    /// <summary>
    /// <see cref="PathNode{TResult}"/>'s build a tree of nodes starting from <see cref="PathNodeTree{T}.rootNode"/>.
    /// The tree is build based on a given list of <see cref="string"/> paths.
    /// 
    /// The route from <see cref="PathNodeTree{T}.rootNode"/> to a leaf node represents a given <see cref="string"/> path.
    /// For each given path a <see cref="PathSelector{T}"/> is created. The <see cref="PathSelector{T}"/> is then added
    /// to <see cref="selectors"/>.
    /// 
    /// The root of the hierarchy is <see cref="PathNodeTree{T}.rootNode"/>
    /// A <see cref="PathSelector{T}"/> is intended to store the result when reaching a node while traversing having an
    /// associated <see cref="PathSelector{T}"/> in <see cref="selectors"/>.
    /// </summary>
    public class PathNode<TResult>
    {
        /// direct access to <see cref="children"/>[*]
        internal            PathNode<TResult>                       wildcardNode;
        internal            int                                     arrayIndex;
        
        internal readonly   List<PathSelector<TResult>>             selectors = new List<PathSelector<TResult>>();
        internal readonly   PathNode<TResult>                       parent;
        
        private  readonly   SelectorNode                            selectorNode;
        private  readonly   List<PathNode<TResult>>                 children = new List<PathNode<TResult>>();
        private  readonly   List<byte[]>                            keys = new List<byte[]>();

        public      bool                            IsMember()      => selectorNode.selectorType == SelectorType.Member;
        public      string                          GetName()       => selectorNode.name;
        public      IEnumerable<PathNode<TResult>>  GetChildren()   => children;

        internal bool FindByBytes(ref Bytes key, out PathNode<TResult> node) {
            for (int n = 0; n < keys.Count; n++) {
                if (key.IsEqualArray(keys[n])) {
                    node = children[n];
                    return true;
                }
            }
            node = null;
            return false;
        }
        
        internal bool FindByIndex(int key, out PathNode<TResult> node) {
            for (int n = 0; n < children.Count; n++) {
                node = children[n];
                if (key == node.selectorNode.index)
                    return true;
            }
            node = null;
            return false;
        }
        
        internal bool FindByString(string key, out PathNode<TResult> node) {
            for (int n = 0; n < children.Count; n++) {
                node = children[n];
                if (key.Equals(node.selectorNode.name))
                    return true;
            }
            node = null;
            return false;
        }

        public override string ToString() {
            var sb = new StringBuilder();
            AscendToString(sb);
            return sb.ToString();
        }

        internal void Add(PathNode<TResult> node) {
            children.Add(node);
            var key = Encoding.UTF8.GetBytes(node.selectorNode.name);
            keys.Add(key);
        }

        internal void Clear() {
            children.Clear();
            keys.Clear();
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
        internal SelectorNode(string name, int index, SelectorType selectorType) {
            this.name           = name;
            this.index          = index;
            this.selectorType   = selectorType;
        }
        
        internal        readonly    string          name;
        internal        readonly    int             index;
        internal        readonly    SelectorType    selectorType;

        private         const       string          Wildcard = "[*]";

        public   override           string          ToString() => name;

        private static void PathNodeToSelectorNode(string path, int start, int end, List<SelectorNode> selectorNodes) {
            int len = end - start;
            var arrayStart = path.IndexOf('[', start);
            var arrayEnd   = path.IndexOf(']', start);
            if (arrayStart != -1 || arrayEnd != -1) {
                if (arrayStart + 2 <= arrayEnd) {
                    SelectorType indexType;
                    string indexString = path.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
                    switch (indexString) {
                        case "*":  indexType = SelectorType.ArrayWildcard;   break;
                        case "=>": indexType = SelectorType.ArrayGroup;      break;
                        default:
                            throw new InvalidOperationException($"unsupported array selector: {path.Substring(start, len)}");
                    }
                    var token = path.Substring(start, arrayStart - start);
                    selectorNodes.Add(new SelectorNode (token,    -1, SelectorType.Member));
                    selectorNodes.Add(new SelectorNode (Wildcard, -1, indexType));
                    return;
                }
                throw new InvalidOperationException($"Invalid array selector: {path.Substring(start, len)}");
            }
            var memberToken = path.Substring(start, len);
            selectorNodes.Add(new SelectorNode (memberToken, -1, SelectorType.Member));
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
        public static string PathToPathTokens(string path, List<JsonKey> pathTokens) {
            pathTokens.Clear();
            int last = 1;
            int len = path.Length;
            if (len == 0)
                return path;
            for (int n = 1; n < len; n++) {
                if (path[n] == '/') {
                    var token = path.Substring(last, n - last);
                    pathTokens.Add(new JsonKey(token));
                    last = n + 1;
                }
            }
            var lastToken = path.Substring(last, len - last);
            pathTokens.Add(new JsonKey(lastToken));
            return path;
        }
    }
}
