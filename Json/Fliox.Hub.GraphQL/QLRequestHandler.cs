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
        
        internal QLRequestContext CreateRequest(
            string          operationName,
            GraphQLDocument document,
            string          docStr,
            out string      error)
        {
            var definitions = document.Definitions;
            var queries     = new List<Query> ();
            var utf8Buffer  = new Utf8Buffer();
            foreach (var definition in definitions) {
                if (!(definition is GraphQLOperationDefinition operation))
                    continue;
                if (operation.Name != operationName)
                    continue;
                var selections  = operation.SelectionSet.Selections;
                error           = AddQueries(selections, docStr, queries, utf8Buffer);
                if (error != null) {
                    return default;
                }
            }
            error       = null;
            var tasks   = new List<SyncRequestTask>(queries.Count);
            foreach (var query in queries) {
                tasks.Add(query.task);   
            }
            var syncRequest = new SyncRequest { database = database, tasks = tasks };
            return new QLRequestContext(syncRequest, queries);
        }
        
        private string AddQueries(List<ASTNode> selections, string docStr, List<Query> queries, IUtf8Buffer buffer)
        {
            queries.Capacity = queries.Count + selections.Count;
            foreach (var selection in selections) {
                if (!(selection is GraphQLField graphQLQuery))
                    continue;
                var name = graphQLQuery.Name.StringValue;
                if (!resolvers.TryGetValue(name, out var resolver)) {
                    continue;
                }
                var queryRequest = CreateQueryTask(resolver, graphQLQuery, docStr, out string error);
                if (error != null)
                    return error;
                var selectionNode   = ResponseUtils.CreateSelection(graphQLQuery, buffer, resolver.objectType);
                var query           = new Query(name, resolver.queryType, resolver.container, queryRequest, selectionNode);
                queries.Add(query);
            }
            return null;
        }
        
        private static QueryRequest CreateQueryTask(
            in QueryResolver    resolver,
            GraphQLField        query,
            string              docStr,
            out string          error)
        {
            switch(resolver.queryType) {
                case QueryType.Query:   return QueryEntities    (resolver, query,           out error);
                case QueryType.Count:   return CountEntities    (resolver, query,           out error);
                case QueryType.Read:    return ReadEntities     (resolver, query,           out error);
                case QueryType.Create:  return CreateEntities   (resolver, query, docStr,   out error);
                case QueryType.Upsert:  return UpsertEntities   (resolver, query, docStr,   out error);
                case QueryType.Delete:  return DeleteEntities   (resolver, query,           out error);
                case QueryType.Command: return SendCommand      (resolver, query, docStr,   out error);
                case QueryType.Message: return SendMessage      (resolver, query, docStr,   out error);
            }
            throw new InvalidOperationException($"unexpected resolver type: {resolver.queryType}");
        }

        private static QueryRequest QueryEntities(in QueryResolver resolver, GraphQLField query, out string error)
        {
            string  filter      = RequestArgs.GetString (query, "filter",   out error);
            if (error != null)
                return default;
            int?    limit       = RequestArgs.GetInt    (query, "limit",    out error);
            if (error != null)
                return default;
            int?    maxCount    = RequestArgs.GetInt    (query, "maxCount", out error);
            if (error != null)
                return default;
            string  cursor      = RequestArgs.GetString (query, "cursor",   out error);
            if (error != null)
                return default;
            bool?   selectAll   = RequestArgs.GetBoolean(query, "selectAll",out error);
            if (error != null)
                return default;
            var task = new QueryEntities {
                container = resolver.container, filter = filter, limit = limit,
                maxCount = maxCount, cursor = cursor
            };
            return new QueryRequest(task, selectAll);
        }
        
        private static QueryRequest CountEntities(in QueryResolver resolver, GraphQLField query, out string error)
        {
            string  filter  = RequestArgs.GetString (query, "filter", out error);
            if (error != null)
                return default;
            var task = new AggregateEntities { container = resolver.container, type = AggregateType.count, filter = filter };
            return new QueryRequest(task);
        }
        
        private static QueryRequest ReadEntities(in QueryResolver resolver, GraphQLField query, out string error)
        {
            var ids = RequestArgs.GetIds(query, out error);
            if (error != null)
                return default;
            bool?   selectAll   = RequestArgs.GetBoolean(query, "selectAll",out error);
            if (error != null)
                return default;
            var sets    = new List<ReadEntitiesSet> { new ReadEntitiesSet { ids = ids } };
            var task    = new ReadEntities { container = resolver.container, sets = sets };
            return new QueryRequest(task, selectAll);
        }
        
        private static QueryRequest CreateEntities(in QueryResolver resolver, GraphQLField query, string docStr, out string error)
        {
            var entities = RequestArgs.GetEntities(query, docStr, out error);
            if (error != null)
                return default;
            var task = new CreateEntities { container = resolver.container, entities = entities };
            return new QueryRequest(task);
        }
        
        private static QueryRequest UpsertEntities(in QueryResolver resolver, GraphQLField query, string docStr, out string error)
        {
            var entities = RequestArgs.GetEntities(query, docStr, out error);
            if (error != null)
                return default;
            var task = new UpsertEntities { container = resolver.container, entities = entities };
            return new QueryRequest(task);
        }
        
        private static QueryRequest DeleteEntities(in QueryResolver resolver, GraphQLField query, out string error)
        {
            var ids = RequestArgs.GetIds(query, out error);
            if (error != null)
                return default;
            var task = new DeleteEntities { container = resolver.container, ids = ids };
            return new QueryRequest(task);
        }
        
        private static QueryRequest SendCommand(in QueryResolver resolver, GraphQLField query, string docStr, out string error)
        {
            var param   = RequestArgs.GetParam(query, docStr, resolver, out error);
            var task    = new SendCommand { name = resolver.name, param = param };
            return new QueryRequest(task);
        }
        
        private static QueryRequest SendMessage(in QueryResolver resolver, GraphQLField query, string docStr, out string error)
        {
            var param   = RequestArgs.GetParam(query, docStr, resolver, out error);
            var task    = new SendMessage { name = resolver.name, param = param };
            return new QueryRequest(task);
        }
    }
    
    internal readonly struct QueryRequest {
        internal  readonly  SyncRequestTask task;
        internal  readonly  bool            selectAll;
        
        internal QueryRequest (SyncRequestTask task, bool? selectAll = false) {
            this.task       = task;
            this.selectAll  = selectAll.HasValue && selectAll.Value;
        }
    }
}

#endif