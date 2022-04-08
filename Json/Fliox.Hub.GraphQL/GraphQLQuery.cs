// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using GraphQLParser.AST;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    public static class GraphQLQuery
    {
        internal static Task Execute(RequestContext context, GraphQLDocument query, DbGraphQLSchema schema) {
            context.WriteError("request", "not implemented", 400);
            return Task.CompletedTask;
        }
    }
}

#endif