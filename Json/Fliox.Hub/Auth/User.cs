// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host.Stats;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Auth
{
    public class User {
        // --- public
        public   readonly   JsonKey             userId;
        public   readonly   string              token;
        public   readonly   Authorizer          authorizer;
        public   readonly   HashSet<JsonKey>    clients = new HashSet<JsonKey>(JsonKey.Equality);
        
        public   override   string              ToString() => userId.AsString();
        
        // --- internal
        // key: database
        internal readonly   ConcurrentDictionary<string, RequestCount>  requestCounts;
        
        public static readonly  JsonKey   AnonymousId = new JsonKey("anonymous");


        internal User (in JsonKey userId, string token, Authorizer authorizer) {
            requestCounts   = new ConcurrentDictionary<string, RequestCount>();
            this.userId     = userId;
            this.token      = token;
            this.authorizer = authorizer;
        }
    }
}
