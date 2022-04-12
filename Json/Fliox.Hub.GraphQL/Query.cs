// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Schema.Definition;
using GraphQLParser.AST;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal readonly struct Query
    {
        internal  readonly  string          name;
        internal  readonly  QueryType       type;
        internal  readonly  string          container;
        internal  readonly  SyncRequestTask task;
        internal  readonly  GraphQLField    graphQL;

        public    override  string          ToString() => $"{type}: {name}";

        internal Query(string name, QueryType type, string container, SyncRequestTask task, GraphQLField graphQL) {
            this.name       = name;
            this.type       = type;
            this.container  = container;
            this.task       = task;
            this.graphQL    = graphQL;
        }
    }
    
    internal readonly struct QueryResolver
    {
        internal  readonly  string      name;
        internal  readonly  QueryType   type;
        /// <summary> only: <see cref="QueryType.Query"/> and <see cref="QueryType.ReadById"/> </summary>
        internal  readonly  string      container;
        /// <summary> only: <see cref="QueryType.Message"/> and <see cref="QueryType.Command"/> </summary>
        internal  readonly  bool        hasParam;
        /// <summary> only: <see cref="QueryType.Message"/> and <see cref="QueryType.Command"/> </summary>
        internal  readonly  bool        paramRequired;  // message / command only

        public    override  string      ToString() => $"{type}: {name}";

        internal QueryResolver(string name, QueryType type, string container, FieldDef param) {
            this.name       = name;
            this.type       = type;
            this.container  = container;
            hasParam        = param != null;
            paramRequired   = param != null && param.required;
        }
    }
    
    internal enum QueryType {
        Query,
        ReadById,
        Command,
        Message
    }
}

#endif