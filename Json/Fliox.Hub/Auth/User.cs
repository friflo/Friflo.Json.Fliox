// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host.Stats;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Auth
{
    public class User {
        // --- public
        public   readonly   JsonKey                             userId;
        public   readonly   string                              token;
        public   readonly   Authorizer                          authorizer;
        public   readonly   HashSet<JsonKey>                    clients = new HashSet<JsonKey>(JsonKey.Equality);
        
        // --- internal
        internal readonly   Dictionary<string, RequestCount>    requestCounts = new Dictionary<string, RequestCount>();

        public   override   string                              ToString() => userId.AsString();
        
        public static readonly  JsonKey   AnonymousId = new JsonKey("anonymous");


        internal User (in JsonKey userId, string token, Authorizer authorizer) {
            this.userId     = userId;
            this.token      = token;
            this.authorizer = authorizer;
        }
    }
}
