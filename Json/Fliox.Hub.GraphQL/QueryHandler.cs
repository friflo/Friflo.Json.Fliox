// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Schema.Definition;
using GraphQLParser.AST;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal class QueryHandler
    {
        private readonly Dictionary<string, Query>  queryTypes = new Dictionary<string, Query>();
        
        internal QueryHandler(TypeSchema typeSchema) {
            var rootType = typeSchema.RootType;
            foreach (var field in rootType.Fields) {
                var container = field.name;
                var query       = new Query(QueryType.Query,    container);
                var readById    = new Query(QueryType.ReadById, container);
                queryTypes.Add(container,           query);
                queryTypes.Add($"{container}ById",  readById);
            }
        }
        
        internal async Task Execute(RequestContext context, GraphQLDocument document) {
            foreach (ASTNode definition in document.Definitions) {
                switch (definition) {
                    case GraphQLOperationDefinition operation:
                        foreach (var selection in operation.SelectionSet.Selections) {
                            switch (selection) {
                                case GraphQLField queryField:
                                    var name = queryField.Name.StringValue;
                                    if (!queryTypes.TryGetValue(name, out var query)) {
                                        context.WriteError("unknown query", name, 400);
                                        continue;
                                    }
                                    switch(query.type) {
                                        case QueryType.Query:
                                            await ExecuteQuery   (context, query, queryField);
                                            break;
                                        case QueryType.ReadById:
                                            await ExecuteReadById(context, query, queryField);
                                            break;
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }
            context.WriteError("request", "not implemented", 400);
        }
        
        private Task ExecuteQuery(RequestContext context, Query query, GraphQLField queryField) {
            var selectionSet    = queryField.SelectionSet;
            if (selectionSet != null) {
                foreach (var selection in selectionSet.Selections) { }
            }
            return Task.CompletedTask;
        }
        
        private Task ExecuteReadById(RequestContext context, Query query, GraphQLField queryField) {
            var selectionSet    = queryField.SelectionSet;
            if (selectionSet != null) {
                foreach (var selection in selectionSet.Selections) { }
            }
            return Task.CompletedTask;
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