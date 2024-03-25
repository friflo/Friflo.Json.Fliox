// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using Friflo.Json.Fliox.Schema.GraphQL;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal sealed class QLDatabaseSchema {
        private     readonly    string              database;
        internal    readonly    string              schemaName;
        internal    readonly    JsonValue           schemaResponse;
        internal    readonly    GqlSchema           schema;
        internal    readonly    QLRequestHandler    requestHandler;

        public      override    string              ToString() => database;

        internal QLDatabaseSchema(
            string              database,
            string              schemaName,
            GqlSchema           schema,
            JsonValue           schemaResponse,
            QLRequestHandler    handler)
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