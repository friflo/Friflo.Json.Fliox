// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Remote;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    public static class GraphQLExtensions
    {
        public static void UseGraphQL(this HttpHost httpHost) {
            httpHost.AddHandler(new GraphQLHandler());
        }
    }
}