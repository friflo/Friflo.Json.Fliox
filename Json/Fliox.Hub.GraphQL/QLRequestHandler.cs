// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
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
            foreach (var field in schemaType.Fields) {
                var container   = field.name;
                var query       = new QueryResolver("query",    QueryType.Query,    container, null);
                var readById    = new QueryResolver("read",     QueryType.ReadById, container, null);
                var create      = new QueryResolver("create",   QueryType.Create,   container, null);
                var upsert      = new QueryResolver("upsert",   QueryType.Upsert,   container, null);
                resolvers.Add(query.name,       query);
                resolvers.Add(readById.name,    readById);
                resolvers.Add(create.name,      create);
                resolvers.Add(upsert.name,      upsert);
            }
            AddMessages(schemaType.Commands, QueryType.Command);
            AddMessages(schemaType.Messages, QueryType.Message);
        }
        
        private void AddMessages(IReadOnlyList<MessageDef> messages, QueryType messageType) {
            if (messages == null)
                return;
            foreach (var message in messages) {
                var name    = message.name.Replace(".", "_");
                var query   = new QueryResolver(message.name, messageType, null, message.param);
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
            var syncRequest = new SyncRequest {
                database    = database,
                tasks       = tasks
            };
            return new QLRequestContext(syncRequest, queries);
        }
        
        private string AddQueries(List<ASTNode> selections, string docStr, List<Query> queries, IUtf8Buffer buffer)
        {
            foreach (var selection in selections) {
                if (!(selection is GraphQLField graphQLQuery))
                    continue;
                var name = graphQLQuery.Name.StringValue;
                if (!resolvers.TryGetValue(name, out var resolver)) {
                    continue;
                }
                var task = CreateQueryTask(resolver, graphQLQuery, docStr, out string error);
                if (error != null)
                    return error;
                var selectionNode   = ResponseUtils.CreateSelection(graphQLQuery, buffer);
                var query           = new Query(name, resolver.type, resolver.container, task, selectionNode);
                queries.Add(query);
            }
            return null;
        }
        
        private static SyncRequestTask CreateQueryTask(
            in QueryResolver    resolver,
            GraphQLField        query,
            string              docStr,
            out string          error)
        {
            switch(resolver.type) {
                case QueryType.Query:       return QueryEntities    (resolver, query,           out error);
                case QueryType.ReadById:    return ReadEntities     (resolver, query,           out error);
                case QueryType.Create:      return CreateEntities   (resolver, query, docStr,   out error);
                case QueryType.Upsert:      return UpsertEntities   (resolver, query, docStr,   out error);
                case QueryType.Command:     return SendCommand      (resolver, query, docStr,   out error);
                case QueryType.Message:     return SendMessage      (resolver, query, docStr,   out error);
            }
            throw new InvalidOperationException($"unexpected resolver type: {resolver.type}");
        }

        private static QueryEntities QueryEntities(in QueryResolver resolver, GraphQLField query, out string error)
        {
            string  filter  = null;
            int?    limit   = null;
            var arguments   = query.Arguments;
            if (arguments != null) {
                foreach (var argument in arguments) {
                    var value   = argument.Value;
                    var argName = argument.Name.StringValue;
                    switch (argName) {
                        case "filter":  filter  = RequestUtils.TryGetStringArg (value, out error);  break;
                        case "limit":   limit   = RequestUtils.TryGetIntArg    (value, out error);  break;
                        default:        error   = RequestUtils.UnknownArgument(argName);            break;
                    }
                    if (error != null)
                        return null;
                }
            }
            error = null;
            return new QueryEntities { container = resolver.container, filter = filter, limit = limit };
        }
        
        private static ReadEntities ReadEntities(in QueryResolver resolver, GraphQLField query, out string error)
        {
            List<JsonKey> idList    = null;
            var arguments = query.Arguments;
            if (arguments != null) {
                foreach (var argument in arguments) {
                    var argName = argument.Name.StringValue;
                    switch (argName) {
                        case "ids":     idList  = RequestUtils.TryGetIdList (argument, out error);  break;
                        default:        error   = RequestUtils.UnknownArgument(argName);            break;
                    }
                    if (error != null)
                        return null;
                }
            }
            var ids     = new HashSet<JsonKey>(idList.Count, JsonKey.Equality);
            foreach (var id in idList) {
                ids.Add(id);
            }
            var sets    = new List<ReadEntitiesSet> { new ReadEntitiesSet { ids = ids } };
            error = null;
            return new ReadEntities { container = resolver.container, sets = sets };
        }
        
        private static List<JsonValue> GetEntities(GraphQLField query, string docStr, out string error)
        {
            List<JsonValue> entities = null;
            var arguments = query.Arguments;
            if (arguments != null) {
                foreach (var argument in arguments) {
                    var argName = argument.Name.StringValue;
                    switch (argName) {
                        case "entities":    entities    = RequestUtils.TryGetAnyList(argument.Value, docStr, out error);    break;
                        default:            error       = RequestUtils.UnknownArgument(argName);                            break;
                    }
                    if (error != null)
                        return null;
                }
            }
            error = null;
            return entities;
        }
        
        private static CreateEntities CreateEntities(in QueryResolver resolver, GraphQLField query, string docStr, out string error)
        {
            var entities = GetEntities(query, docStr, out error);
            if (error != null)
                return null;
            return new CreateEntities { container = resolver.container, entities = entities };
        }
        
        private static UpsertEntities UpsertEntities(in QueryResolver resolver, GraphQLField query, string docStr, out string error)
        {
            var entities = GetEntities(query, docStr, out error);
            if (error != null)
                return null;
            return new UpsertEntities { container = resolver.container, entities = entities };
        }
        
        private static SendCommand SendCommand(in QueryResolver resolver, GraphQLField query, string docStr, out string error)
        {
            var arguments   = query.Arguments;
            var param       = GetMessageArg(arguments, docStr, resolver, out error);
            return new SendCommand { name = resolver.name, param = param };
        }
        
        private static SendMessage SendMessage(in QueryResolver resolver, GraphQLField query, string docStr, out string error)
        {
            var arguments   = query.Arguments;
            var param       = GetMessageArg(arguments, docStr, resolver, out error);
            return new SendMessage { name = resolver.name, param = param };
        }
        
        private static JsonValue GetMessageArg(GraphQLArguments args, string docStr, in QueryResolver resolver, out string error) {
            if (args == null) {
                if (!resolver.hasParam) {
                    error = null;
                    return new JsonValue();
                }
                if (resolver.paramRequired) {
                    error = "Expect argument: param";
                } else {
                    error = null;
                }
                return new JsonValue();
            }
            JsonValue result;
            foreach (var argument in args) {
                var argName = argument.Name.StringValue;
                switch (argName) {
                    case "param":   result  = RequestUtils.TryGetAny(argument.Value, docStr, out error);    break;
                    default:        error   = RequestUtils.UnknownArgument(argName);                        break;
                }
                if (error != null)
                    return new JsonValue();
            }
            error = null;
            return result;
        }
    }
}

#endif