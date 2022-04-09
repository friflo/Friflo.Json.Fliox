// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Remote;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class ResponseHandler
    {
        public static void ProcessSyncResponse(RequestContext context, SyncRequest syncRequest, SyncResponse syncResponse) {
            context.WriteError("response error", "ResponseHandler not implemented", 400);
        }
    }
}