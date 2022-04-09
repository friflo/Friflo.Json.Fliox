// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Remote;
using GraphQLParser.AST;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class ResponseHandler
    {
        public static QueryResult ProcessSyncResponse(
            RequestContext      context,
            List<GraphQLField>  queries,
            SyncRequest         syncRequest,
            SyncResponse        syncResponse)
        {
            var tasks   = syncRequest.tasks;
            var results = syncResponse.tasks;
            for (int n = 0; n < tasks.Count; n++) {
                var task    = tasks[n];
                var result  = results[n];
            }
            return new QueryResult("response error", "ResponseHandler not implemented", 400);
        }
    }
}

#endif
