// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Definition;
using GraphQLParser.AST;

// ReSharper disable InlineOutVariableDeclaration
// ReSharper disable PossibleNullReferenceException
namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal sealed class QLRequestHandler
    {
        private  readonly   ShortString                             database;
        private  readonly   Dictionary<ShortString, QueryResolver>  resolvers = new Dictionary<ShortString, QueryResolver>(ShortString.Equality);
        
        internal QLRequestHandler(TypeSchema typeSchema, string database) {
            this.database   = new ShortString(database);
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
                var name        = message.name.Replace(".", "_");
                var resultType  = message.result?.type;
                var query       = new QueryResolver(message.name, messageType, message.param, resultType);
                resolvers.Add(new ShortString(name), query);
            }
        }
        
        internal QLRequestContext CreateRequest(ObjectMapper mapper, GqlRequest gqlRequest, GraphQLDocument document, string doc)
        {
            var definitions = document.Definitions;
            var queries     = new List<Query> ();
            var utf8Buffer  = new Utf8Buffer();
            var tasks       = new ListOne<SyncRequestTask>();
            foreach (var definition in definitions) {
                if (!(definition is GraphQLOperationDefinition operation))
                    continue;
                if (operation.Name != gqlRequest.operationName)
                    continue;
                var selections  = operation.SelectionSet.Selections;
                AddQueries(mapper, selections, doc, gqlRequest.variables, queries, tasks, utf8Buffer);
            }
            var syncRequest = new SyncRequest { database = database, tasks = tasks };
            return new QLRequestContext(syncRequest, queries);
        }
        
        private void AddQueries(
            ObjectMapper                    mapper,
            List<ASTNode>                   selections,
            string                          doc,
            Dictionary<string, JsonValue>   variables,
            List<Query>                     queries,
            ListOne<SyncRequestTask>        tasks,
            IUtf8Buffer                     buffer)
        {
            queries.Capacity    = queries.Count + selections.Count;
            tasks.Capacity      = tasks.Count   + selections.Count;
            foreach (var selection in selections) {
                if (!(selection is GraphQLField graphQLQuery))
                    continue;
                var name    = graphQLQuery.Name.StringValue;
                var alias   = graphQLQuery.Alias?.Name.StringValue;
                if (!resolvers.TryGetValue(new ShortString(name), out var resolver)) {
                    QueryRequest queryRequest = new QueryError(null, $"unknown query / mutation: {name}");
                    var query = new Query(name, alias, resolver.queryType, resolver.container, default, -1, queryRequest);
                    queries.Add(query);
                } else {
                    var cx              = new QueryContext(mapper, resolver, graphQLQuery, doc, variables);
                    var queryRequest    = CreateQueryTask(cx);
                    var task            = queryRequest.task;
                    var taskIndex       = task == null ? -1 : tasks.Count;
                    if (task != null) {
                        tasks.Add(queryRequest.task);
                    }
                    var selectionNode   = ResponseUtils.CreateSelection(graphQLQuery, buffer, resolver.resultObject);
                    var query           = new Query(name, alias, resolver.queryType, resolver.container, selectionNode, taskIndex, queryRequest);
                    queries.Add(query);
                }
            }
        }
        
        private static QueryRequest CreateQueryTask(in QueryContext cx)
        {
            switch(cx.resolver.queryType) {
                case QueryType.Query:   return QueryEntities    (cx);
                case QueryType.Count:   return CountEntities    (cx);
                case QueryType.Read:    return ReadEntities     (cx);
                case QueryType.Create:  return CreateEntities   (cx);
                case QueryType.Upsert:  return UpsertEntities   (cx);
                case QueryType.Delete:  return DeleteEntities   (cx);
                case QueryType.Command: return SendCommand      (cx);
                case QueryType.Message: return SendMessage      (cx);
            }
            throw new InvalidOperationException($"unexpected resolver type: {cx.resolver.queryType}");
        }

        private static QueryRequest QueryEntities(in QueryContext cx)
        {
            QueryError? error;
            if (!RequestArgs.TryGetFilter   (cx, "filter",     out var filter,     out error)) return error;
            if (!RequestArgs.TryGetInt      (cx, "limit",      out var limit,      out error)) return error;
            if (!RequestArgs.TryGetInt      (cx, "maxCount",   out var maxCount,   out error)) return error;
            if (!RequestArgs.TryGetString   (cx, "cursor",     out var cursor,     out error)) return error;
            if (!RequestArgs.TryGetBool     (cx, "selectAll",  out var selectAll,  out error)) return error;
            if (!RequestArgs.TryGetEnumValue(cx, "orderByKey", out var order,      out error)) return error;

            SortOrder orderByKey =  order switch {
                "asc"   => SortOrder.asc,
                "desc"  => SortOrder.desc,
                _       => default
            };
            var task = new QueryEntities {
                container = cx.resolver.container, filter = filter, limit = limit, maxCount = maxCount, cursor = cursor, orderByKey = orderByKey
            };
            return new QueryRequest(task, selectAll);
        }
        
        private static QueryRequest CountEntities(in QueryContext cx)
        {
            QueryError? error;
            if (!RequestArgs.TryGetFilter (cx, "filter", out string filter,  out error)) return error;
            
            var task = new AggregateEntities {
                container = cx.resolver.container, type = AggregateType.count, filter = filter
            };
            return new QueryRequest(task);
        }
        
        private static QueryRequest ReadEntities(in QueryContext cx)
        {
            QueryError? error;
            if (!RequestArgs.TryGetIds  (cx, "ids",       out var ids,       out error)) return error;
            if (!RequestArgs.TryGetBool (cx, "selectAll", out var selectAll, out error)) return error;
            
            var task    = new ReadEntities { container = cx.resolver.container, ids = ids };
            return new QueryRequest(task, selectAll);
        }
        
        private static QueryRequest CreateEntities(in QueryContext cx)
        {
            var entities = RequestArgs.GetEntities(cx, out var error);
            if (error != null)
                return error;
            var task = new CreateEntities { container = cx.resolver.container, entities = entities };
            return new QueryRequest(task);
        }
        
        private static QueryRequest UpsertEntities(in QueryContext cx)
        {
            var entities = RequestArgs.GetEntities(cx, out var error);
            if (error != null)
                return error;
            var task = new UpsertEntities { container = cx.resolver.container, entities = entities };
            return new QueryRequest(task);
        }
        
        private static QueryRequest DeleteEntities(in QueryContext cx)
        {
            if (!RequestArgs.TryGetIds(cx, "ids", out var ids, out var error))
                return error;
            var task = new DeleteEntities { container = cx.resolver.container, ids = ids };
            return new QueryRequest(task);
        }
        
        private static QueryRequest SendCommand(in QueryContext cx)
        {
            var param   = RequestArgs.GetParam(cx, out var error);
            if (error != null)
                return error;
            var task    = new SendCommand { name = cx.resolver.name, param = param };
            return new QueryRequest(task);
        }
        
        private static QueryRequest SendMessage(in QueryContext cx)
        {
            var param   = RequestArgs.GetParam(cx, out var error);
            if (error != null)
                return error;
            var task    = new SendMessage { name = cx.resolver.name, param = param };
            return new QueryRequest(task);
        }
    }
    
    internal readonly struct QueryContext {
        private   readonly  ObjectMapper                    mapper;
        internal  readonly  QueryResolver                   resolver;
        internal  readonly  GraphQLField                    query;
        internal  readonly  string                          doc;
        private   readonly  Dictionary<string, JsonValue>   variables;

        public    override  string          ToString() => resolver.ToString();

        internal QueryContext (ObjectMapper mapper, QueryResolver resolver, GraphQLField query, string doc, Dictionary<string, JsonValue> variables) {
            this.mapper     = mapper;
            this.resolver   = resolver;
            this.query      = query;
            this.doc        = doc;
            this.variables  = variables;
        }
        
        internal T ReadVariable<T>(in QueryContext cx, GraphQLVariable variable, string name, out QueryError? error) {
            if (variables == null) {
                error = RequestUtils.QueryError(name, "variable not found", variable, cx.doc);
                return default;
            }
            var varName = variable.Name.StringValue;
            if (!variables.TryGetValue(varName, out var jsonValue)) {
                error = RequestUtils.QueryError(name, "variable not found", variable, cx.doc);
                return default;
            }
            var reader = cx.mapper.reader;
            var result = reader.Read<T>(jsonValue);
            if (reader.Error.ErrSet) {
                error = RequestUtils.QueryError(name, reader.Error.GetMessageBody(), variable, cx.doc);
                return default;
            }
            error = null;
            return result;
        }
        
        // ReSharper disable once UnusedMember.Local
        private string Full { get {
            var loc = query.Location;
            return doc.Substring(loc.Start, loc.End - loc.Start);
        } } 
    }
}

#endif