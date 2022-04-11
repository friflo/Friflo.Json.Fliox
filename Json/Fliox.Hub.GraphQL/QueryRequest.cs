// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Definition;
using GraphQLParser.AST;

// ReSharper disable PossibleNullReferenceException
namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal readonly struct QueryRequest
    {
        internal readonly   SyncRequest     syncRequest;
        internal readonly   List<Query>     queries;
        
        internal QueryRequest (SyncRequest syncRequest, List<Query> queries) {
            this.syncRequest    = syncRequest;
            this.queries        = queries;
        }
    }
    
    
    internal class QueryRequestHandler
    {
        private readonly string                             database;
        private readonly Dictionary<string, QueryResolver>  resolvers = new Dictionary<string, QueryResolver>();
        
        internal QueryRequestHandler(TypeSchema typeSchema, string database) {
            this.database   = database;
            var schemaType  = typeSchema.RootType;
            foreach (var field in schemaType.Fields) {
                var container = field.name;
                var query       = new QueryResolver(container,          QueryType.Query,    container, null);
                var readById    = new QueryResolver($"{container}ById", QueryType.ReadById, container, null);
                resolvers.Add(query.name,       query);
                resolvers.Add(readById.name,    readById);
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
        
        internal List<QueryRequest> CreateRequests(RequestContext context, GraphQLDocument document, string docStr, out string error) {
            var definitions = document.Definitions;
            var result      = new List<QueryRequest>(definitions.Count);
            foreach (var definition in definitions) {
                switch (definition) {
                    case GraphQLOperationDefinition operation:
                        var selections  = operation.SelectionSet.Selections;
                        var queries     = new List<Query> (selections.Count);
                        var syncRequest = CreateSyncRequest(context, selections, docStr, queries, out error);
                        if (error != null) {
                            return null;
                        }
                        result.Add(new QueryRequest(syncRequest, queries));
                        break;
                }
            }
            error = null;
            return result;
        }
        
        private SyncRequest CreateSyncRequest(
            RequestContext  context,
            List<ASTNode>   selections,
            string          docStr,
            List<Query>     queries,
            out string      error)
        {
            error       = null;
            foreach (var selection in selections) {
                switch (selection) {
                    case GraphQLField graphQLQuery:
                        var name = graphQLQuery.Name.StringValue;
                        if (!resolvers.TryGetValue(name, out var resolver)) {
                            continue;
                        }
                        var task = CreateQueryTask(resolver, graphQLQuery, docStr, out error);
                        if (error != null)
                            return null;
                        var query = new Query(name, resolver.type, resolver.container, task, graphQLQuery);
                        queries.Add(query);
                        break;
                }
            }
            var userId  = context.cookies["fliox-user"];
            var token   = context.cookies["fliox-token"];
            var tasks   = new List<SyncRequestTask>(queries.Count);
            foreach (var query in queries) {
                tasks.Add(query.task);   
            }
            return new SyncRequest {
                database    = database,
                tasks       = tasks,
                userId      = new JsonKey(userId),
                token       = token
            };
        }
        
        private static SyncRequestTask CreateQueryTask(
            in QueryResolver    resolver,
            GraphQLField        query,
            string              docStr,
            out string          error)
        {
            switch(resolver.type) {
                case QueryType.Query:       return QueryEntities(resolver, query, out error);
                case QueryType.ReadById:    return ReadEntities (resolver, query, out error);
                case QueryType.Command:     return SendCommand  (resolver, query, docStr, out error);
                case QueryType.Message:     return SendMessage  (resolver, query, docStr, out error);
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
                        case "filter":  filter  = AstUtils.TryGetStringArg (value, out error);  break;
                        case "limit":   limit   = AstUtils.TryGetIntArg    (value, out error);  break;
                        default:        error   = AstUtils.UnknownArgument(argName);            break;
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
                        case "ids":     idList  = AstUtils.TryGetIdList (argument, out error);  break;
                        default:        error   = AstUtils.UnknownArgument(argName);            break;
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
                    case "param":   result  = AstUtils.TryGetAny(argument.Value, docStr, out error);    break;
                    default:        error   = AstUtils.UnknownArgument(argName);                        break;
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