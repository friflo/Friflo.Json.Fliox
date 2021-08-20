// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Transform.Select;

namespace Friflo.Json.Flow.Transform
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class ScalarSelect
    {
        internal readonly   PathNodeTree<ScalarSelectResult>    nodeTree = new PathNodeTree<ScalarSelectResult>();
        internal readonly   List<ScalarSelectResult>            results = new List<ScalarSelectResult>();
        
        public              List<ScalarSelectResult>            Results => results;

        
        internal ScalarSelect() { }
        
        public ScalarSelect(string selector) {
            var selectors = new[] {selector};
            CreateNodeTree(selectors);
            results.Capacity = selectors.Length;
        }
        
        public ScalarSelect(IList<string> selectors) {
            CreateNodeTree(selectors);
            results.Capacity = selectors.Count;
        }

        internal void CreateNodeTree(IList<string> pathList) {
            nodeTree.CreateNodeTree(pathList);
            foreach (var selector in nodeTree.selectors) {
                selector.result = new ScalarSelectResult ();
            }
        }
        
        internal void InitSelectorResults() {
            foreach (var selector in nodeTree.selectors) {
                selector.result.Init();
            }
        }
    }
    
    // --- Select result ---
    public class ScalarSelectResult
    {
        public   readonly   List<Scalar>    values          = new List<Scalar>();
        public   readonly   List<int>       groupIndices    = new List<int>();
        private             int             lastGroupIndex;

        internal void Init() {
            values.Clear();
            groupIndices.Clear();
            lastGroupIndex = -1;
        }

        internal static void Add(Scalar scalar, List<PathSelector<ScalarSelectResult>> selectors) {
            foreach (var selector in selectors) {
                var parentGroup = selector.parentGroup;
                var result = selector.result;
                if (parentGroup != null) {
                    var index = parentGroup.arrayIndex;
                    if (index != result.lastGroupIndex) {
                        result.lastGroupIndex = index;
                        result.groupIndices.Add(result.values.Count);
                    }
                }
                result.values.Add(scalar);
            }
        }

        public List<string> AsStrings() {
            var result = new List<string>(values.Count);
            foreach (var item in values) {
                var str = item.AsString();
                if (str != null)
                    result.Add(str);
            }
            return result;
        }
        
        public List<object> AsObjects() {
            var result = new List<object>(values.Count);
            foreach (var item in values) {
                result.Add(item.AsObject());
            }
            return result;
        }
    }
}