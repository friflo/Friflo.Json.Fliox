// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class ResponseHandler
    {
        internal static QueryResult ProcessSyncResponse(
            RequestContext  context,
            List<Query>     queries,
            SyncResponse    syncResponse)
        {
            var results = syncResponse.tasks;
            for (int n = 0; n < queries.Count; n++) {
                var query   = queries[n];
                var result  = results[n];
                switch (query.type) {
                    case QueryType.Query:
                        QueryEntities(query, (QueryEntitiesResult)result);
                        break;
                    case QueryType.ReadById:
                        ReadEntities(query, (ReadEntitiesResult)result);
                        break;
                }
            }
            return new QueryResult("response error", "ResponseHandler not implemented", 400);
        }
        
        private static void QueryEntities(Query query, QueryEntitiesResult result) {
            
        }
        
        private static void ReadEntities(Query query, ReadEntitiesResult result) {
            
        }
    }
}

#endif
