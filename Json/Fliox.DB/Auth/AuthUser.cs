// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Auth
{
    public class AuthUser {
        internal readonly   JsonKey                                     userId;
        internal readonly   string                                      token;
        internal readonly   Authorizer                                  authorizer;
        internal readonly   HashSet<JsonKey>                            clients = new HashSet<JsonKey>(JsonKey.Equality);
        internal readonly   Dictionary<EntityDatabase, RequestStats>    dbStats = new Dictionary<EntityDatabase, RequestStats>();

        public   override   string              ToString() => userId.AsString();

        internal AuthUser (in JsonKey userId, string token, Authorizer authorizer) {
            this.userId     = userId;
            this.token      = token;
            this.authorizer = authorizer;
        }
    }
    
    public struct RequestStats {
        public            int                 requests;
        public            int                 tasks;
        
        internal static void Update (Dictionary<EntityDatabase, RequestStats> dbStats, EntityDatabase db, SyncRequest syncRequest) {
            if (!dbStats.TryGetValue(db, out RequestStats stats)) {
                dbStats.TryAdd(db, new RequestStats());
            }
            stats.requests  ++;
            stats.tasks     += syncRequest.tasks.Count;
            dbStats[db] = stats;
        }
    }
}