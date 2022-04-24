// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Schema.Definition;
using GraphQLParser.AST;

// ReSharper disable InlineOutVariableDeclaration
// ReSharper disable PossibleNullReferenceException
namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal class QLRequestHandler
    {
        private  readonly   string                              database;
        private  readonly   Dictionary<string, QueryResolver>   resolvers = new Dictionary<string, QueryResolver>();
        
        internal QLRequestHandler(TypeSchema typeSchema, string database) {
            this.database   = database;
            var schemaType  = typeSchema.RootType;
            var buffer      = new Utf8Buffer();
            foreach (var field in schemaType.Fields) {
                var container   = field.name;
                var entityType  = field.type;
                var query   = new QueryResolver("query",    QueryType.Query,    container, entityType,  buffer);
                var count   = new QueryResolver("count",    QueryType.Count,    container, null,        buffer);
                var read    = new QueryResolver("read",     QueryType.Read,     container, entityType,  buffer);
                var create  = new QueryResolver("create",   QueryType.Create,   container, null,        buffer);
                var upsert  = new QueryResolver("upsert",   QueryType.Upsert,   container, null,        buffer);
                var delete  = new QueryResolver("delete",   QueryType.Delete,   container, null,        buffer);
                resolvers.Add(query.name,   query);
                resolvers.Add(count.name,   count);
                resolvers.Add(read.name,    read);
                resolvers.Add(create.name,  create);
                resolvers.Add(upsert.name,  upsert);
                resolvers.Add(delete.name,  delete);
            }
            AddMessages(schemaType.Commands, QueryType.Command);
            AddMessages(schemaType.Messages, QueryType.Message);
        }
        
        private void AddMessages(IReadOnlyList<MessageDef> messages, QueryType messageType) {
            if (messages == null)
                return;
            foreach (var message in messages) {
                var name    = message.name.Replace(".", "_");
                var type    = message.result.type;
                var query   = new QueryResolver(message.name, messageType, message.param, type);
                resolvers.Add(name,             query);
            }
        }
        
        internal QLRequestContext CreateRequest(string operationName, GraphQLDocument document, string doc)
        {
            var definitions = document.Definitions;
            var queries     = new List<Query> ();
            var utf8Buffer  = new Utf8Buffer();
            var tasks       = new List<SyncRequestTask>();
            foreach (var definition in definitions) {
                if (!(definition is GraphQLOperationDefinition operation))
                    continue;
                if (operation.Name != operationName)
                    continue;
                var selections  = operation.SelectionSet.Selections;
                AddQueries(selections, doc, queries, tasks, utf8Buffer);
            }
            var syncRequest = new SyncRequest { database = database, tasks = tasks };
            return new QLRequestContext(syncRequest, queries);
        }
        
        private void AddQueries(
            List<ASTNode>           selections,
            string                  doc,
            List<Query>             queries,
            List<SyncRequestTask>   tasks,
            IUtf8Buffer             buffer)
        {
            queries.Capacity = queries.Count + selections.Count;
            foreach (var selection in selections) {
                if (!(selection is GraphQLField graphQLQuery))
                    continue;
                var name = graphQLQuery.Name.StringValue;
                if (!resolvers.TryGetValue(name, out var resolver)) {
                    QueryRequest queryRequest = new QueryError(null, "unknown query / mutation");
                    var query = new Query(name, resolver.queryType, resolver.container, default, -1, queryRequest);
                    queries.Add(query);
                } else {
                    var queryRequest    = CreateQueryTask(resolver, graphQLQuery, doc);
                    var task            = queryRequest.task;
                    var taskIndex       = task == null ? -1 : tasks.Count;
                    if (task != null) {
                        tasks.Add(queryRequest.task);
                    }
                    var selectionNode   = ResponseUtils.CreateSelection(graphQLQuery, buffer, resolver.objectType);
                    var query           = new Query(name, resolver.queryType, resolver.container, selectionNode, taskIndex, queryRequest);
                    queries.Add(query);
                }
            }
        }
        
        private static QueryRequest CreateQueryTask(
            in QueryResolver    resolver,
            GraphQLField        query,
            string              doc)
        {
            switch(resolver.queryType) {
                case QueryType.Query:   return QueryEntities    (resolver, query, doc);
                case QueryType.Count:   return CountEntities    (resolver, query, doc);
                case QueryType.Read:    return ReadEntities     (resolver, query, doc);
                case QueryType.Create:  return CreateEntities   (resolver, query, doc);
                case QueryType.Upsert:  return UpsertEntities   (resolver, query, doc);
                case QueryType.Delete:  return DeleteEntities   (resolver, query, doc);
                case QueryType.Command: return SendCommand      (resolver, query, doc);
                case QueryType.Message: return SendMessage      (resolver, query, doc);
            }
            throw new InvalidOperationException($"unexpected resolver type: {resolver.queryType}");
        }

        private static QueryRequest QueryEntities(in QueryResolver resolver, GraphQLField query, string doc)
        {
            QueryError? error;
            if (!RequestArgs.TryGetFilter (query, "filter",     out var filter,     out error, doc)) return error;
            if (!RequestArgs.TryGetInt    (query, "limit",      out var limit,      out error, doc)) return error;
            if (!RequestArgs.TryGetInt    (query, "maxCount",   out var maxCount,   out error, doc)) return error;
            if (!RequestArgs.TryGetString (query, "cursor",     out var cursor,     out error, doc)) return error;
            if (!RequestArgs.TryGetBool   (query, "selectAll",  out var selectAll,  out error, doc)) return error;
            
            var task = new QueryEntities {
                container = resolver.container, filter = filter, limit = limit,
                maxCount = maxCount, cursor = cursor
            };
            return new QueryRequest(task, selectAll);
        }
        
        private static QueryRequest CountEntities(in QueryResolver resolver, GraphQLField query, string doc)
        {
            QueryError? error;
            if (!RequestArgs.TryGetFilter (query, "filter", out string filter,  out error, doc)) return error;
            
            var task = new AggregateEntities { container = resolver.container, type = AggregateType.count, filter = filter };
            return new QueryRequest(task);
        }
        
        private static QueryRequest ReadEntities(in QueryResolver resolver, GraphQLField query, string doc)
        {
            QueryError? error;
            if (!RequestArgs.TryGetIds  (query, "ids",       out var ids,       out error, doc)) return error;
            if (!RequestArgs.TryGetBool (query, "selectAll", out var selectAll, out error, doc)) return error;
            
            var sets    = new List<ReadEntitiesSet> { new ReadEntitiesSet { ids = ids } };
            var task    = new ReadEntities { container = resolver.container, sets = sets };
            return new QueryRequest(task, selectAll);
        }
        
        private static QueryRequest CreateEntities(in QueryResolver resolver, GraphQLField query, string doc)
        {
            var entities = RequestArgs.GetEntities(query, out var error, doc);
            if (error != null)
                return error;
            var task = new CreateEntities { container = resolver.container, entities = entities };
            return new QueryRequest(task);
        }
        
        private static QueryRequest UpsertEntities(in QueryResolver resolver, GraphQLField query, string doc)
        {
            var entities = RequestArgs.GetEntities(query, out var error, doc);
            if (error != null)
                return error;
            var task = new UpsertEntities { container = resolver.container, entities = entities };
            return new QueryRequest(task);
        }
        
        private static QueryRequest DeleteEntities(in QueryResolver resolver, GraphQLField query, string doc)
        {
            if (!RequestArgs.TryGetIds(query, "ids", out var ids, out var error, doc))
                return error;
            var task = new DeleteEntities { container = resolver.container, ids = ids };
            return new QueryRequest(task);
        }
        
        private static QueryRequest SendCommand(in QueryResolver resolver, GraphQLField query, string doc)
        {
            var param   = RequestArgs.GetParam(query, resolver, out var error, doc);
            if (error != null)
                return error;
            var task    = new SendCommand { name = resolver.name, param = param };
            return new QueryRequest(task);
        }
        
        private static QueryRequest SendMessage(in QueryResolver resolver, GraphQLField query, string doc)
        {
            var param   = RequestArgs.GetParam(query, resolver, out var error, doc);
            if (error != null)
                return error;
            var task    = new SendMessage { name = resolver.name, param = param };
            return new QueryRequest(task);
        }
    }
}

#endif