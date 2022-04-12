// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal struct SelectionNode
    {
        internal SelectionNode (in Query query) {
            var set = query.graphQL.SelectionSet;
            if (set == null) {
                return;
            }
            foreach (var selection in set.Selections) {
                
            }
        }
    }
}

#endif