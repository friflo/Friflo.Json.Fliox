﻿// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Transform.Select
{
    internal sealed class PathSelector<T>
    {
        private  readonly   string          path;
        private  readonly   PathNode<T>     node; // only used to give context when debugging
        private  readonly   bool            isArrayResult;
        internal readonly   PathNode<T>     parentGroup;
        internal            T               result;
        
        internal            string          Path => path;

        public override string ToString() => path;
        
        internal PathSelector(string path, PathNode<T> node, bool isArrayResult, PathNode<T> parentGroup) {
            this.path           = path;
            this.node           = node;
            this.isArrayResult  = isArrayResult;
            this.parentGroup    = parentGroup;
        }
    }
}
