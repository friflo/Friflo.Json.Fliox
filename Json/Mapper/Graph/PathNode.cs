// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
    
    internal class SelectQuery {
        internal readonly   string          path;
        internal readonly   StringBuilder   arrayResult;
        internal            string          jsonResult;
        internal            int             itemCount;

        internal SelectQuery(string path, StringBuilder arrayResult) {
            this.path           = path;
            this.arrayResult    = arrayResult;
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
        
        internal readonly   string         name;
        internal readonly   SelectorType   selectorType;
    }
    
    public class PathSelector {
        internal readonly   PathNode            rootNode = new PathNode(new SelectorNode("root", SelectorType.Root));
        internal readonly   List<SelectQuery>   selectList = new List<SelectQuery>();
        private  readonly   List<SelectorNode>  selectorNodes = new List<SelectorNode>(); // reused buffer

        internal PathSelector() { }
        
        public PathSelector(IList<string> pathList) {
            CreateSelector(pathList);
        }

        public IList<string> GetResult() {
            var result = selectList.Select(select => {
                var arrayResult = select.arrayResult;
                if (arrayResult != null) {
                    arrayResult.Append(']');
                    return arrayResult.ToString();
                }
                return select.jsonResult;
            }).ToList();
            return result;
        }

        internal void CreateSelector(IList<string> pathList) {
            selectList.Clear();
            rootNode.children.Clear();
            var isArrayResult = false;
            var count = pathList.Count;
            for (int n = 0; n < count; n++) {
                var path = pathList[n];
                PathNode.PathToSelectorNodes(path, selectorNodes);
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

                StringBuilder arrayResult = isArrayResult ? new StringBuilder() : null;
                var select = new SelectQuery (path, arrayResult);
                curNode.select = select;
                selectList.Add(select);
            }
        }
        
        internal void InitSelector() {
            foreach (var select in selectList) {
                var sb = select.arrayResult;
                if (sb != null) {
                    sb.Clear();
                    sb.Append('[');
                }
                select.itemCount = 0;
                select.jsonResult = null;
            }
        }
    }
}