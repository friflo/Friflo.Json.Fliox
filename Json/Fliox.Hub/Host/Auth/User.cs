// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Friflo.Json.Fliox.Hub.Host.Stats;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public class User {
        // --- public
        public   readonly   JsonKey     userId;
        public   readonly   string      token;
        public   readonly   Authorizer  authorizer;
        
        public   override   string      ToString() => userId.AsString();
        
        // --- internal
        internal readonly   ConcurrentDictionary<JsonKey, Empty>        clients;        // key: clientId
        internal readonly   ConcurrentDictionary<string, RequestCount>  requestCounts;  // key: database
        
        public static readonly  JsonKey   AnonymousId = new JsonKey("anonymous");


        internal User (in JsonKey userId, string token, Authorizer authorizer) {
            clients         = new ConcurrentDictionary<JsonKey, Empty>(JsonKey.Equality);
            requestCounts   = new ConcurrentDictionary<string, RequestCount>();
            this.userId     = userId;
            this.token      = token;
            this.authorizer = authorizer;
        }
    }
    
    internal struct Empty { }
}
