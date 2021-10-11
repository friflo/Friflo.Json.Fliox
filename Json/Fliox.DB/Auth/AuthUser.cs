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
        internal readonly   Dictionary<EntityDatabase, RequestStats>    stats = new Dictionary<EntityDatabase, RequestStats>();

        public   override   string              ToString() => userId.AsString();

        internal AuthUser (in JsonKey userId, string token, Authorizer authorizer) {
            this.userId     = userId;
            this.token      = token;
            this.authorizer = authorizer;
        }
    }
    
    public struct RequestStats {
        public            string              database;
        public            int                 requests;
        public            int                 tasks;
        
        internal static void Update (Dictionary<EntityDatabase, RequestStats> stats, EntityDatabase db, SyncRequest syncRequest) {
            if (!stats.TryGetValue(db, out RequestStats requestStats)) {
                requestStats = new RequestStats { database = db.name };
                stats.TryAdd(db, requestStats);
            }
            requestStats.requests  ++;
            requestStats.tasks     += syncRequest.tasks.Count;
            stats[db] = requestStats;
        }
    }
}