// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform.Project;
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
                writer.Pretty           = false;
                writer.WriteNullMembers = false;
                for (int n = 0; n < queries.Count; n++) {
                    var query       = queries[n];
                    var taskResult  = taskResults[n];
                    var queryResult = ProcessTaskResult(query, taskResult, writer, syncResponse);
                    data.Add(query.name, queryResult);
                }
                var response            = new GqlResponse { data = data };
                writer.Pretty           = true;
                return new JsonValue(writer.WriteAsArray(response));
            }
        }
        
        private static JsonValue ProcessTaskResult(in Query query, SyncTaskResult result, ObjectWriter writer, SyncResponse synResponse) {
            if (result is TaskErrorResult taskError) {
                return new JsonValue(writer.WriteAsArray(taskError));
            }
            switch (query.type) {
                case QueryType.Query:       return QueryEntitiesResult  (query, result, writer, synResponse);
                case QueryType.ReadById:    return ReadEntitiesResult   (query, result, writer, synResponse);
                case QueryType.Create:      return CreateEntitiesResult (query, result, writer);
                case QueryType.Upsert:      return UpsertEntitiesResult (query, result, writer);
                case QueryType.Command:     return SendCommandResult    (query, result, writer);
                case QueryType.Message:     return SendMessageResult    (query, result, writer);
            }
            throw new InvalidOperationException($"unexpected query type: {query.type}");
        }
        
        private static JsonValue QueryEntitiesResult(in Query query, SyncTaskResult result, ObjectWriter writer, SyncResponse synResponse) {
            var queryResult     = (QueryEntitiesResult)result;
            var entities        = synResponse.resultMap[query.container].entityMap;
            var ids             = queryResult.ids;
            var list            = new List<JsonValue>(ids.Count);
            foreach (var id in ids) {
                var entity = entities[id].Json;
                list.Add(entity);
            }
            var json            = new JsonValue(writer.WriteAsArray(list));
            var projector       = new JsonProjector();
            return projector.Project(query.selection, json);
        }
        
        private static JsonValue ReadEntitiesResult (in Query query, SyncTaskResult result, ObjectWriter writer, SyncResponse synResponse) {
            var readTask        = (ReadEntities)query.task;
            var entities        = synResponse.resultMap[query.container].entityMap;
            var ids             = readTask.sets[0].ids;
            var list            = new List<JsonValue>(ids.Count);
            foreach (var id in ids) {
                var entity = entities[id].Json;
                list.Add(entity);
            }
            var json            = new JsonValue(writer.WriteAsArray(list));
            var projector       = new JsonProjector();
            return projector.Project(query.selection, json);
        }
        
        private static JsonValue CreateEntitiesResult(in Query query, SyncTaskResult result, ObjectWriter writer) {
            var createResult    = (CreateEntitiesResult)result;
            return new JsonValue("{}"); // todo return errors
        }
        
        private static JsonValue UpsertEntitiesResult(in Query query, SyncTaskResult result, ObjectWriter writer) {
            var upsertResult    = (UpsertEntitiesResult)result;
            return new JsonValue("{}"); // todo return errors
        }
        
        private static JsonValue SendCommandResult  (in Query query, SyncTaskResult result, ObjectWriter writer) {
            var commandResult   = (SendCommandResult)result;
            var projector       = new JsonProjector();
            return projector.Project(query.selection, commandResult.result);
        }
        
        private static JsonValue SendMessageResult  (in Query query, SyncTaskResult result, ObjectWriter writer) {
            return new JsonValue("{}");
        }
    }
}
