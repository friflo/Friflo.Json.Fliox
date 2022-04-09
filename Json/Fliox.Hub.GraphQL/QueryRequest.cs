// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Definition;
using GraphQLParser.AST;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal class QueryRequest
    {
        private readonly string                             database;
        private readonly Dictionary<string, QueryResolver>  resolvers = new Dictionary<string, QueryResolver>();
        
        internal QueryRequest(TypeSchema typeSchema, string database) {
            this.database   = database;
            var rootType    = typeSchema.RootType;
            foreach (var field in rootType.Fields) {
                var container = field.name;
                var query       = new QueryResolver(container,          QueryType.Query,    container);
                var readById    = new QueryResolver($"{container}ById", QueryType.ReadById, container);
                resolvers.Add(query.name,       query);
                resolvers.Add(readById.name,    readById);
            }
        }
        
        internal async Task<QueryResult> Execute(RequestContext context, GraphQLDocument document) {
            foreach (var definition in document.Definitions) {
                switch (definition) {
                    case GraphQLOperationDefinition operation:
                        var selections  = operation.SelectionSet.Selections;
                        var queries     = new List<Query> (selections.Count);
                        var syncRequest = CreateSyncRequest(context, selections, queries, out string error);
                        if (error != null) {
                            return new QueryResult("query error", error, 400);
                        }
                        var response = await context.ExecuteSyncRequest(syncRequest).ConfigureAwait(false);
                        
                        if (response.error != null) {
                            return new QueryResult("request error", response.error.message, 400);
                        }
                        return QueryResponse.ProcessSyncResponse(context, queries, response.success);
                }
            }
            return new QueryResult ("request", "not implemented", 400);
        }
        
        private SyncRequest CreateSyncRequest(
            RequestContext  context,
            List<ASTNode>   selections,
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
                        SyncRequestTask task = null;
                        switch(resolver.type) {
                            case QueryType.Query:
                                task = QueryEntities(resolver, graphQLQuery, out error);
                                break;
                            case QueryType.ReadById:
                                task = ReadEntities (resolver, graphQLQuery, out error);
                                break;
                        }
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
        
        private static QueryEntities QueryEntities(QueryResolver resolver, GraphQLField query, out string error)
        {
            string  filter  = null;
            int?    limit   = null;
            var arguments   = query.Arguments;
            if (arguments != null) {
                foreach (var argument in arguments) {
                    var value = argument.Value;
                    switch (argument.Name.StringValue) {
                        case "filter":  
                            if (!AstUtils.TryGetStringArg (value, out filter, out error))
                                return null;
                            break;
                        case "limit":
                            if (!AstUtils.TryGetIntArg (value, out limit, out error))
                                return null;
                            break;
                    }
                }
            }
            error = null;
            return new QueryEntities { container = resolver.container, filter = filter, limit = limit };
        }
        
        private static ReadEntities ReadEntities(QueryResolver resolver, GraphQLField query, out string error)
        {
            error                   = null;
            List<JsonKey> idList    = null;
            var arguments = query.Arguments;
            if (arguments != null) {
                foreach (var argument in arguments) {
                    switch (argument.Name.StringValue) {
                        case "ids":
                            if (!AstUtils.TryGetIdList (argument, out idList, out error))
                                return null;
                            break;
                    }
                }
            }
            if (idList == null)
                return null;
            var ids     = new HashSet<JsonKey>(idList.Count, JsonKey.Equality);
            foreach (var id in idList) {
                ids.Add(id);
            }
            var sets    = new List<ReadEntitiesSet> { new ReadEntitiesSet { ids = ids } };
            error = null;
            return new ReadEntities { container = resolver.container, sets = sets };
        }
    }
}

#endif