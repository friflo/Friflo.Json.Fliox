// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.GraphQL;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal class GraphQLDbSchema {
        private     readonly    string              database;
        internal    readonly    string              schemaName;
        internal    readonly    JsonValue           schemaResponse;
        internal    readonly    GqlSchema           schema;
        internal    readonly    QueryRequestHandler requestHandler;

        public      override    string          ToString() => database;

        internal GraphQLDbSchema(
            string              database,
            string              schemaName,
            GqlSchema           schema,
            JsonValue           schemaResponse,
            QueryRequestHandler handler)
        {
            this.database       = database;
            this.schemaName     = schemaName;
            this.schema         = schema;
            this.schemaResponse = schemaResponse;
            this.requestHandler = handler;
        } 
    }
}

#endif