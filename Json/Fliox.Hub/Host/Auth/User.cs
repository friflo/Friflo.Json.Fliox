// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Fliox.Hub.DB.Cluster;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    /// <summary>
    /// A <see cref="User"/> instance store credentials, <see cref="clients"/> and permissions of a user. <br/>
    /// Permissions: <br/>
    /// <see cref="taskAuthorizer"/> to authorize task execution.<br/>
    /// <see cref="hubPermission"/> for general - non task specific - permissions.<br/>
    /// </summary>
    /// <remarks>
    /// <b>Important:</b> <see cref="User"/> instances must be used only within the execution of a single <see cref="Protocol.SyncRequest"/>. <br/>
    /// Caching them may result in dealing with outdated <see cref="User"/> instances as new instances created by an
    /// <see cref="Authenticator"/> whenever its credentials or permissions changes.
    /// </remarks>
    public sealed class User {
        // --- public
        public   readonly   ShortString     userId;
        public   readonly   string          token;
        internal            TaskAuthorizer  taskAuthorizer  = TaskAuthorizer.None;  // not null
        internal            HubPermission   hubPermission   = HubPermission.None;   // not null

        public   override   string          ToString() => userId.AsString();
        
        // --- internal
        internal readonly   ConcurrentDictionary<ShortString, Empty>    clients;        // key: clientId
        /// <b>Note</b> requires lock when accessing. Did not use ConcurrentDictionary to avoid heap allocation
        internal readonly   Dictionary<ShortString, RequestCount>       requestCounts;  // key: database
        private             HashSet<ShortString>                        groups;         // can be null
        
        public static readonly  ShortString   AnonymousId = new ShortString("anonymous");


        internal User (in ShortString userId, string token) {
            clients             = new ConcurrentDictionary<ShortString, Empty>(ShortString.Equality);
            requestCounts       = new Dictionary<ShortString, RequestCount>   (ShortString.Equality);
            this.userId         = userId;
            this.token          = token;
        }
        
        public  IReadOnlyCollection<ShortString> GetGroups() {
            if (groups != null)
                return groups;
            return Array.Empty<ShortString>();
        }
        
        internal void SetGroups(IReadOnlyCollection<ShortString> groups) {
            this.groups = groups?.ToHashSet(ShortString.Equality);
        }
        
        public void SetUserOptions(UserParam param) {
            groups = UpdateGroups(groups, param);
        }
        
        public static HashSet<ShortString> UpdateGroups(ICollection<ShortString> groups, UserParam param) {
            var result = groups != null ? new HashSet<ShortString>(groups, ShortString.Equality) : new HashSet<ShortString>(ShortString.Equality);
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
        
    internal enum PreAuthType
    {
        None            = 0,
        MissingUserId   = 1,
        MissingToken    = 2,
        Unknown         = 3,
        Failed          = 4,
        Success         = 5,
    }
    
    internal struct Empty { }
}
