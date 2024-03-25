// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    public static class GraphQLExtensions
    {
        /// <summary>
        /// Provide access to <see cref="HttpHost"/> using <b>GraphQL</b>
        /// </summary>
        /// <remarks>
        /// For each database a GraphQL schema is generated based on the <see cref="DatabaseSchema"/> assigned to each
        /// <see cref="EntityDatabase"/>.
        /// </remarks>
        public static void UseGraphQL(this HttpHost httpHost) {
            httpHost.AddHandler(new GraphQLHandler());
        }
    }
}