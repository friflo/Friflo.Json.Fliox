// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if UNITY_5_3_OR_NEWER

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    public sealed class GraphQLHandler: IRequestHandler
    {
        public string[]     Routes                                  => new [] { "/graphql" };
        public bool         IsMatch       (RequestContext context)  => false;
        public Task<bool>   HandleRequest (RequestContext context)  => throw new NotImplementedException("GraphQLHandler");
    }
}

#endif
