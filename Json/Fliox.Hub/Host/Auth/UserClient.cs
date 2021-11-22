// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Friflo.Json.Fliox.Hub.Host.Stats;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public readonly struct UserClient {
        internal readonly   JsonKey                                     userId;
        internal readonly   ConcurrentDictionary<string, RequestCount>  requestCounts;
        
        public   override   string                                      ToString() => userId.AsString();

        internal UserClient (in JsonKey userId) {
            requestCounts   = new ConcurrentDictionary<string, RequestCount>();
            this.userId     = userId;
        }
    }
}