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
    internal class QueryHandler
    {
        private readonly string                     database;
        private readonly Dictionary<string, Query>  queryTypes = new Dictionary<string, Query>();
        
        internal QueryHandler(TypeSchema typeSchema, string database) {
            this.database   = database;
            var rootType    = typeSchema.RootType;
            foreach (var field in rootType.Fields) {
                var container = field.name;
                var query       = new Query(QueryType.Query,    container);
                var readById    = new Query(QueryType.ReadById, container);
                queryTypes.Add(container,           query);
                queryTypes.Add($"{container}ById",  readById);
            }
        }
        
        internal async Task Execute(RequestContext context, GraphQLDocument document) {
            foreach (var definition in document.Definitions) {
                switch (definition) {
                    case GraphQLOperationDefinition operation:
                        var selections  = operation.SelectionSet.Selections;
                        var syncRequest = CreateSyncRequest(context, selections, out string error);
                        if (error != null) {
                            context.WriteError("query error", error, 400);
                            return;
                        }
                        var response = await context.ExecuteSyncRequest(syncRequest);
                        
                        if (response.error != null) {
                            context.WriteError("request error", response.error.message, 400);
                            return;
                        }
                        ResponseHandler.ProcessSyncResponse(context, syncRequest, response.success);
                        return;
                }
            }
            context.WriteError("request", "not implemented", 400);
        }
        
        private SyncRequest CreateSyncRequest(RequestContext context, List<ASTNode> selections, out string error) {
            error       = null;
            var tasks   = new List<SyncRequestTask>(selections.Count);
            foreach (var selection in selections) {
                switch (selection) {
                    case GraphQLField queryField:
                        var name = queryField.Name.StringValue;
                        if (!queryTypes.TryGetValue(name, out var query)) {
                            continue;
                        }
                        SyncRequestTask task = null;
                        switch(query.type) {
                            case QueryType.Query:
                                task = CreateQuery   (query, queryField, out error);
                                break;
                            case QueryType.ReadById:
                                task = CreateReadById(query, queryField, out error);
                                break;
                        }
                        if (error != null)
                            return null;
                        tasks.Add(task);
                        break;
                }
            }
            var userId  = context.cookies["fliox-user"];
            var token   = context.cookies["fliox-token"];
            return new SyncRequest {
                database    = database,
                tasks       = tasks,
                userId      = new JsonKey(userId),
                token       = token
            };
        }
        
        private static QueryEntities CreateQuery(Query query, GraphQLField queryField, out string error)
        {
            string  filter  = null;
            int?    limit   = null; 
            var arguments   = queryField.Arguments;
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
            return new QueryEntities { container = query.container, filter = filter, limit = limit };
        }
        
        private static ReadEntities CreateReadById(Query query, GraphQLField queryField, out string error)
        {
            error                   = null;
            List<JsonKey> idList    = null;
            var arguments = queryField.Arguments;
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
            return new ReadEntities { container = query.container, sets = sets };
        }
    }
    
    internal class Query
    {
        internal  readonly  QueryType   type;
        internal  readonly  string      container;

        public    override  string      ToString() => container;

        internal Query(QueryType type, string container) {
            this.type       = type;
            this.container  = container;
        }
    }
    
    internal enum QueryType {
        Query,
        ReadById
    }
}

#endif