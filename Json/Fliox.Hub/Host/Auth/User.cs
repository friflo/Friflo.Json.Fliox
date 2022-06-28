// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.DB.Monitor;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class User {
        // --- public
        public   readonly   JsonKey     userId;
        public   readonly   string      token;
        public   readonly   Authorizer  authorizer;
        public              string[]    Groups => groups != null ? groups.ToArray() : Array.Empty<string>();

        public   override   string      ToString() => userId.AsString();
        
        // --- internal
        internal readonly   ConcurrentDictionary<JsonKey, Empty>        clients;        // key: clientId
        internal readonly   ConcurrentDictionary<string, RequestCount>  requestCounts;  // key: database
        internal            HashSet<string>                             groups;
        
        public static readonly  JsonKey   AnonymousId = new JsonKey("anonymous");


        internal User (in JsonKey userId, string token, Authorizer authorizer) {
            clients         = new ConcurrentDictionary<JsonKey, Empty>(JsonKey.Equality);
            requestCounts   = new ConcurrentDictionary<string, RequestCount>();
            this.userId     = userId;
            this.token      = token;
            this.authorizer = authorizer;
        }
        
        public void SetUserOptions(UserOptions options) {
            var addGroups = options.addGroups;
            if (addGroups != null) {
                if (groups == null) {
                    groups = new HashSet<string>(addGroups);
                } else {
                    groups.UnionWith(addGroups);
                }
            }
            var removeGroups = options.removeGroups;
            if (removeGroups != null && groups != null) {
                foreach (var item in removeGroups) {
                    groups.Remove(item);                    
                }
            }
        }
    }
    
    internal struct Empty { }
}
