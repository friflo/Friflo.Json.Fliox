// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal readonly struct QLRequestContext
    {
        internal readonly   SyncRequest     syncRequest;
        internal readonly   List<Query>     queries;
        
        internal QLRequestContext (SyncRequest syncRequest, List<Query> queries) {
            this.syncRequest    = syncRequest;
            this.queries        = queries;
        }
    }
}

#endif
