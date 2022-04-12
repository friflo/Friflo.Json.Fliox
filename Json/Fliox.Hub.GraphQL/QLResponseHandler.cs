// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class QLResponseHandler
    {
        internal static JsonValue ProcessResponse(
            ObjectPool<ObjectMapper>    mapper,
            List<Query>                 queries,
            SyncResponse                syncResponse)
        {
            var data        = new Dictionary<string, JsonValue>(queries.Count);
            var taskResults = syncResponse.tasks;
            using (var pooled = mapper.Get()) {
                var writer              = pooled.instance.writer;
                writer.Pretty           = true;
                writer.WriteNullMembers = false;
                for (int n = 0; n < queries.Count; n++) {
                    var query       = queries[n];
                    var taskResult  = taskResults[n];
                    var queryResult = ProcessTaskResult(query, taskResult, writer);
                    data.Add(query.name, queryResult);
                }
                var response            = new GqlResponse { data = data };
                return new JsonValue(writer.WriteAsArray(response));
            }
        }
        
        private static JsonValue ProcessTaskResult(in Query query, SyncTaskResult result, ObjectWriter writer) {
            if (result is TaskErrorResult taskError) {
                return new JsonValue(writer.WriteAsArray(taskError));
            }
            switch (query.type) {
                case QueryType.Query:       return QueryEntitiesResult  (query, result, writer);
                case QueryType.ReadById:    return ReadEntitiesResult   (query, result, writer);
                case QueryType.Command:     return SendCommandResult    (query, result, writer);
                case QueryType.Message:     return SendMessageResult    (query, result, writer);
            }
            throw new InvalidOperationException($"unexpected query type: {query.type}");
        }
        
        private static JsonValue QueryEntitiesResult(Query query, SyncTaskResult result, ObjectWriter writer) {
            var selectionNode   = new SelectionNode(query);
            var filter          = new SelectionFilter();
            return filter.Filter(selectionNode, new JsonValue("{}"));
        }
        
        private static JsonValue ReadEntitiesResult (Query query, SyncTaskResult result, ObjectWriter writer) {
            var selectionNode   = new SelectionNode(query);
            var filter          = new SelectionFilter();
            return filter.Filter(selectionNode, new JsonValue("{}"));
        }
        
        private static JsonValue SendCommandResult  (Query query, SyncTaskResult result, ObjectWriter writer) {
            var commandResult   = (SendCommandResult)result;
            var selectionNode   = new SelectionNode(query);
            var filter          = new SelectionFilter();
            return filter.Filter(selectionNode, commandResult.result);
        }
        
        private static JsonValue SendMessageResult  (Query query, SyncTaskResult result, ObjectWriter writer) {
            return new JsonValue("{}");
        }
    }
}

#endif
