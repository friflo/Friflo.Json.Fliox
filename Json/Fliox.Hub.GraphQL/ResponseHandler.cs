// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class ResponseHandler
    {
        internal static QueryResult ProcessSyncResponse(
            RequestContext  context,
            List<Query>     queries,
            SyncResponse    syncResponse)
        {
            var data        = new Dictionary<string, JsonValue>(queries.Count);
            var taskResults = syncResponse.tasks;
            for (int n = 0; n < queries.Count; n++) {
                var query       = queries[n];
                var taskResult  = taskResults[n];
                JsonValue queryResult;
                switch (query.type) {
                    case QueryType.Query:
                        queryResult = QueryEntities(query, (QueryEntitiesResult)taskResult);
                        break;
                    case QueryType.ReadById:
                        queryResult = ReadEntities(query, (ReadEntitiesResult)taskResult);
                        break;
                }
                data.Add(query.name, queryResult);
            }
            var pool = context.Pool;
            using (var pooled = pool.ObjectMapper.Get()) {
                var writer              = pooled.instance.writer;
                writer.Pretty           = true;
                writer.WriteNullMembers = false;
                
                var response            = new GqlResponse { data = data };
                var jsonResponse        = new JsonValue(writer.WriteAsArray(response));

                context.Write(jsonResponse, 0, "application/json", 200);
            }
            return default;
        }
        
        private static JsonValue QueryEntities(Query query, QueryEntitiesResult result) {
            return new JsonValue("{}");
        }
        
        private static JsonValue ReadEntities(Query query, ReadEntitiesResult result) {
            return new JsonValue("{}");
        }
    }
}

#endif
