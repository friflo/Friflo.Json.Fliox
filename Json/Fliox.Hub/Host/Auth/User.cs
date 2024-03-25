// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
        /** not null */ public readonly ShortString                     userId;
        /** not null */ public          TaskAuthorizer                  TaskAuthorizer  => taskAuthorizer;
        /** not null */ public          HubPermission                   HubPermission   => hubPermission;
        /** not null */ public          IReadOnlyCollection<string>     Roles           => roles ?? Array.Empty<string>();

                        public override string                          ToString()      => userId.AsString();

        // --- internal
        /** nullable */ [Browse(Never)] internal    ShortString         token;
        /** not null */ [Browse(Never)] internal    TaskAuthorizer      taskAuthorizer  = TaskAuthorizer.None;
        /** not null */ [Browse(Never)] internal    HubPermission       hubPermission   = HubPermission.None;
        /** nullable */ [Browse(Never)] internal    string[]            roles;
                                        internal    bool                invalidated     = true;

        // --- internal
        internal readonly   ConcurrentDictionary<ShortString, Empty>    clients;        // key: clientId
        /// <b>Note</b> requires lock when accessing. Did not use ConcurrentDictionary to avoid heap allocation
        internal readonly   Dictionary<ShortString, RequestCount>       requestCounts;  // key: database
        private             HashSet<string>                             groups;         // can be null
        
        public static readonly  string   AnonymousId = "anonymous";

        internal User (string  userId) : this (new ShortString(userId)) { }
        
        internal User (in ShortString  userId) {
            if (userId.IsNull()) throw new ArgumentNullException(nameof(userId));
            this.userId         = userId;
            clients             = new ConcurrentDictionary<ShortString, Empty>(ShortString.Equality);
            requestCounts       = new Dictionary<ShortString, RequestCount>   (ShortString.Equality);
        }
        
        internal User Set (in ShortString token, TaskAuthorizer taskAuthorizer, HubPermission hubPermission, string[] roles) {
            this.token          = token;
            this.taskAuthorizer = taskAuthorizer ?? throw new ArgumentNullException(nameof(taskAuthorizer));
            this.hubPermission  = hubPermission  ?? throw new ArgumentNullException(nameof(hubPermission));
            this.roles          = roles;
            invalidated         = false;
            return this;
        }
        
        public  IReadOnlyCollection<string> GetGroups() {
            if (groups != null)
                return groups;
            return Array.Empty<string>();
        }
        
        internal void SetGroups(IReadOnlyCollection<string> groups) {
            this.groups = groups?.Count > 0 ? groups.ToHashSet() : null;
        }
        
        public void SetUserOptions(UserParam param) {
            groups = UpdateGroups(groups, param);
        }
        
        public static HashSet<string> UpdateGroups(ICollection<string> groups, UserParam param) {
            var result = groups != null ? new HashSet<string>(groups) : new HashSet<string>();
            if (param.addGroups != null) {
                result.UnionWith(param.addGroups);
            }
            if (param.removeGroups != null) {
                foreach (var item in param.removeGroups) {
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
