// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Transform.Select;

namespace Friflo.Json.Fliox.Mapper
{
    public sealed class MemberAccess
    {
        internal readonly   PathNodeTree<MemberValue>    nodeTree = new PathNodeTree<MemberValue>();
        internal readonly   List<MemberValue>            results = new List<MemberValue>();
        
        // public           List<ObjectSelectResult>            Results => results;
        
        public MemberAccess(IList<string> selectors) {
            CreateNodeTree(selectors);
            results.Capacity = selectors.Count;
        }
        
        private void CreateNodeTree(IList<string> pathList) {
            nodeTree.CreateNodeTree(pathList);
            foreach (var selector in nodeTree.selectors) {
                selector.result = new MemberValue ();
            }
        }
        
        internal void InitSelectorResults() {
            foreach (var selector in nodeTree.selectors) {
                selector.result.Init(selector.Path);
            }
        }
    }
    
    // --- Select result ---
    public class MemberValue
    {
        internal    JsonUtf8    json;
        internal    object      value;
        private     string      path;
        public      bool        Found { get; internal set; }
        
        public      JsonUtf8    Json    => Found ? json  : throw new InvalidOperationException($"member not found. path: {path}");
        public      object      Value   => Found ? value : throw new InvalidOperationException($"member not found. path: {path}");

        internal void Init(string path) {
            this.path   = path;
            Found       = false;
            value       = null;
        }
    }
}