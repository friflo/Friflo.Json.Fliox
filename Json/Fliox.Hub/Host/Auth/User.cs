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
    /// <summary>
    /// Prover access to data of an authenticated user 
    /// </summary>
    /// <remarks>
    /// <b>Important:</b> <see cref="User"/> instances must be used only within the execution of a single <see cref="Protocol.SyncRequest"/>. <br/>
    /// Caching them may result in dealing with outdated <see cref="User"/> instances as new instances created by an
    /// <see cref="Authenticator"/> whenever its credentials or permissions changes.
    /// </remarks>
    public sealed class User {
        // --- public
        public   readonly   JsonKey                     userId;
        public   readonly   string                      token;
        public   readonly   TaskAuthorizer              taskAuthorizer;     // not null
        public   readonly   HubPermission               hubPermission;      // not null

        public   override   string                      ToString() => userId.AsString();
        
        // --- internal
        internal readonly   ConcurrentDictionary<JsonKey, Empty>        clients;        // key: clientId
        internal readonly   ConcurrentDictionary<string, RequestCount>  requestCounts;  // key: database
        private             HashSet<string>                             groups;         // can be null
        
        public static readonly  JsonKey   AnonymousId = new JsonKey("anonymous");


        internal User (in JsonKey userId, string token, TaskAuthorizer taskAuthorizer, HubPermission hubPermission) {
            clients             = new ConcurrentDictionary<JsonKey, Empty>(JsonKey.Equality);
            requestCounts       = new ConcurrentDictionary<string, RequestCount>();
            this.userId         = userId;
            this.token          = token;
            this.taskAuthorizer = taskAuthorizer;
            this.hubPermission  = hubPermission ?? HubPermission.None;
        }
        
        public  IReadOnlyCollection<string> GetGroups() {
            if (groups != null)
                return groups;
            return Array.Empty<string>();
        }
        
        internal void SetGroups(IReadOnlyCollection<string> groups) {
            this.groups = groups?.ToHashSet();
        }
        
        public void SetUserOptions(UserParam param) {
            groups = UpdateGroups(groups, param);
        }
        
        public static HashSet<string> UpdateGroups(ICollection<string> groups, UserParam param) {
            var result = groups != null ? new HashSet<string>(groups) : new HashSet<string>();
            var addGroups = param.addGroups;
            if (addGroups != null) {
                result.UnionWith(addGroups);
            }
            var removeGroups = param.removeGroups;
            if (removeGroups != null) {
                foreach (var item in removeGroups) {
                    result.Remove(item);                    
                }
            }
            return result;
        }
    }
    
    internal struct Empty { }
}
