// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote;

#if UNITY_5_3_OR_NEWER

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    public class GraphQLHandler: IRequestHandler
    {
        public bool IsMatch       (RequestContext context)  => false;
        public Task HandleRequest (RequestContext context)  => throw new NotImplementedException("GraphQLHandler");
    }
}

#endif
