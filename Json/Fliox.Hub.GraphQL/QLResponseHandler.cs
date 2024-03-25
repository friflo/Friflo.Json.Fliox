// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
        internal static JsonValue Process(
            ObjectMapper                mapper,
            ObjectPool<JsonProjector>   projectorPool,
            List<Query>                 queries,
            SyncResponse                syncResponse)
        {
            using (var pooledProjector  = projectorPool.Get()) {
                var projector           = pooledProjector.instance;
                var writer              = mapper.writer;
                writer.Pretty           = false;
                writer.WriteNullMembers = false;
                return ProcessQueries(writer, projector, queries, syncResponse);
            }
        }
        private static JsonValue ProcessQueries(
            ObjectWriter    writer,
            JsonProjector   projector,
            List<Query>     queries,
            SyncResponse    syncResponse)
        {
            var             data        = new Dictionary<string, JsonValue>(queries.Count);
            List<GqlError>  errors      = null;
            var             taskResults = syncResponse.tasks;

            for (int n = 0; n < queries.Count; n++) {
                var query   = queries[n];
                var name    = query.name; 
                if (query.error != null) {
                    // --- request error
                    var queryError  = query.error.Value;
                    if (errors == null) { errors = new List<GqlError>(); }
                    var path    = new List<string>(2) { name };
                    if (queryError.argName != null)
                        path.Add(queryError.argName);
                    var ext     = new GqlErrorExtensions { type =  TaskErrorType.InvalidTask };
                    var error   = new GqlError { message = queryError.message, path = path, extensions = ext };
                    errors.Add(error);
                    continue;
                }
                var taskResult  = taskResults[query.taskIndex];
                if (!(taskResult is TaskErrorResult taskError)) {
                    // --- success
                    var context     = new ResultContext (query, taskResult, writer, projector, syncResponse);
                    var queryResult = ProcessTaskResult(context);
                    data.Add(name, queryResult);
                } else {
                    // --- response error
                    if (errors == null) { errors = new List<GqlError>(); }
                    var path    = new List<string> { name };
                    var ext     = new GqlErrorExtensions { type = taskError.type, stacktrace = taskError.stacktrace};
                    var error   = new GqlError { message = taskError.message, path = path, extensions = ext };
                    errors.Add(error);
                }
            }
            var response    = new GqlResponse { data = data, errors = errors };
            writer.Pretty   = true;
            return writer.WriteAsValue(response);
        }
        
        private static JsonValue ProcessTaskResult(in ResultContext context) {
            switch (context.query.type) {
                case QueryType.Query:   return QueryEntitiesResult  (context);
                case QueryType.Count:   return CountEntitiesResult  (context);
                case QueryType.Read:    return ReadEntitiesResult   (context);
                case QueryType.Create:  return CreateEntitiesResult (context);
                case QueryType.Upsert:  return UpsertEntitiesResult (context);
                case QueryType.Delete:  return DeleteEntitiesResult (context);
                case QueryType.Command: return SendCommandResult    (context);
                case QueryType.Message: return SendMessageResult    (context);
            }
            throw new InvalidOperationException($"unexpected query type: {context.query.type}");
        }
        
        private static JsonValue QueryEntitiesResult(in ResultContext cx) {
            var queryResult     = (QueryEntitiesResult)cx.result;
            var values          = queryResult.entities.Values;
            var count           = values.Length;
            var items           = new List<JsonValue>(count);
            var gqlQueryResult  = new GqlQueryResult { count = count, cursor = queryResult.cursor, items = items };
            foreach (var value in values) {
                items.Add(value.Json);
            }
            var json            = cx.writer.WriteAsValue(gqlQueryResult);
            if (cx.query.selectAll)
                return json;
            return cx.projector.Project(cx.query.selection, json);
        }
        
        private static JsonValue CountEntitiesResult(in ResultContext cx) {
            var countTask        = (AggregateEntitiesResult)cx.result;
            var count = countTask.value.ToString();
            return new JsonValue(count);
        }
        
        private static JsonValue ReadEntitiesResult (in ResultContext cx) {
            var readResult      = (ReadEntitiesResult)cx.result;
            var values          = readResult.entities.Values;
            var list            = new List<JsonValue>(values.Length);
            foreach (var value in values) {
                list.Add(value.Json);
            }
            var json            = cx.writer.WriteAsValue(list);
            if (cx.query.selectAll)
                return json;
            return cx.projector.Project(cx.query.selection, json);
        }
        
        private static JsonValue CreateEntitiesResult(in ResultContext cx) {
            var createResult    = (CreateEntitiesResult)cx.result;
            var json            = cx.writer.WriteAsValue(createResult.errors);
            return cx.projector.Project(cx.query.selection, json);
        }
        
        private static JsonValue UpsertEntitiesResult(in ResultContext cx) {
            var upsertResult    = (UpsertEntitiesResult)cx.result;
            var json            = cx.writer.WriteAsValue(upsertResult.errors);
            return cx.projector.Project(cx.query.selection, json);
        }
        
        private static JsonValue DeleteEntitiesResult (in ResultContext cx) {
            var deleteResult    = (DeleteEntitiesResult)cx.result;
            var json            = cx.writer.WriteAsValue(deleteResult.errors);
            return cx.projector.Project(cx.query.selection, json);
        }
        
        private static JsonValue SendCommandResult  (in ResultContext cx) {
            var commandResult   = (SendCommandResult)cx.result;
            return cx.projector.Project(cx.query.selection, commandResult.result);
        }
        
        private static JsonValue SendMessageResult  (in ResultContext cx) {
            return new JsonValue("{}");
        }
    }
    
    internal readonly struct ResultContext
    {
        internal  readonly  Query           query;
        internal  readonly  SyncTaskResult  result;
        internal  readonly  ObjectWriter    writer;
        internal  readonly  JsonProjector   projector;
        internal  readonly  SyncResponse    synResponse;

        public    override  string          ToString() => query.name;

        internal ResultContext(Query query, SyncTaskResult result, ObjectWriter writer, JsonProjector projector, SyncResponse synResponse) {
            this.query          = query;
            this.result         = result;
            this.writer         = writer;
            this.projector      = projector;
            this.synResponse    = synResponse;
        }
    }
}
