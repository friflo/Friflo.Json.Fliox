// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    /// <summary>
    /// A <see cref="User"/> instance store credentials, <see cref="clients"/> and permissions of a user. <br/>
    /// Permissions: <br/>
    /// <see cref="TaskAuthorizer"/> to authorize task execution.<br/>
    /// <see cref="HubPermission"/> for general - non task specific - permissions.<br/>
    /// </summary>
    public sealed class User {
        // --- public
        /** not null */ public readonly ShortString             userId;
        /** not null */ public          TaskAuthorizer          TaskAuthorizer  => taskAuthorizer;
        /** not null */ public          HubPermission           HubPermission   => hubPermission;
        /** not null */ public          IReadOnlyList<string>   Roles           => roles ?? Array.Empty<string>();

                        public override string                  ToString() => userId.AsString();

        // --- internal
        /** nullable */ [Browse(Never)] internal            ShortString     token;
        /** not null */ [Browse(Never)] internal            TaskAuthorizer  taskAuthorizer = TaskAuthorizer.None;
        /** not null */ [Browse(Never)] internal            HubPermission   hubPermission  = HubPermission.None;
        /** nullable */ [Browse(Never)] internal            string[]        roles;
                                        internal            bool            invalidated = true;

        // --- internal
        internal readonly   ConcurrentDictionary<ShortString, Empty>    clients;        // key: clientId
        /// <b>Note</b> requires lock when accessing. Did not use ConcurrentDictionary to avoid heap allocation
        internal readonly   Dictionary<ShortString, RequestCount>       requestCounts;  // key: database
        private             HashSet<ShortString>                        groups;         // can be null
        
        public static readonly  ShortString   AnonymousId = new ShortString("anonymous");

        internal User (in ShortString  userId) {
            if (userId.IsNull()) throw new ArgumentNullException(nameof(userId));
            this.userId         = userId;
            clients             = new ConcurrentDictionary<ShortString, Empty>(ShortString.Equality);
            requestCounts       = new Dictionary<ShortString, RequestCount>   (ShortString.Equality);
        }
        
        internal User Set (in ShortString token, TaskAuthorizer taskAuthorizer, HubPermission hubPermission, List<string> roles) {
            this.token          = token;
            this.taskAuthorizer = taskAuthorizer ?? throw new ArgumentNullException(nameof(taskAuthorizer));
            this.hubPermission  = hubPermission  ?? throw new ArgumentNullException(nameof(hubPermission));
            this.roles          = roles?.ToArray();
            invalidated         = false;
            return this;
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
        Failed          = 3,
        Success         = 4,
        UserUnknown     = 5,
        UserInvalidated = 6,
    }
    
    internal struct Empty { }
}
