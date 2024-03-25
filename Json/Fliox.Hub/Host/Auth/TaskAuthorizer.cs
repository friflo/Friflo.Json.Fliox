// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.


using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    /// <summary>
    /// Used to authorize <see cref="SyncRequestTask"/>'s.<br/>
    /// All <see cref="TaskAuthorizer"/> implementations are immutable.
    /// </summary>
    /// <remarks>
    /// This <see cref="TaskAuthorizer"/> it stored at <see cref="AuthState.taskAuthorizer"/>.
    /// The <see cref="AuthState.taskAuthorizer"/> is set via <see cref="Authenticator.AuthenticateAsync"/> for
    /// <see cref="AuthState.authenticated"/> and for not <see cref="AuthState.authenticated"/> users.
    /// </remarks> 
    public abstract class TaskAuthorizer
    {
        public static readonly TaskAuthorizer Full = new AuthorizeGrant();
        public static readonly TaskAuthorizer None = new AuthorizeDeny ();

        /// <summary>
        /// Create a set of <paramref name="databaseFilters"/> used to filter
        /// <see cref="DB.Cluster.ClusterStore"/> read and query results available to a user.
        /// </summary>
        public abstract void    AddAuthorizedDatabases  (HashSet<DatabaseFilter> databaseFilters);
        public abstract bool    AuthorizeTask           (SyncRequestTask task, SyncContext syncContext);
        
        internal static TaskAuthorizer ToAuthorizer(IReadOnlyList<TaskAuthorizer> authorizers) {
            if (authorizers == null)
                return None;
            if (authorizers.Count == 0)
                return None;
            return TrimAny(authorizers);
        }

        private static TaskAuthorizer TrimAny(IReadOnlyList<TaskAuthorizer> list) {
            while (true) {
                if (list.Count == 1) {
                    var single = list[0];
                    if (single is AuthorizeAny any) {
                        list = any.list;
                        continue;
                    }
                    return single;
                }
                return new AuthorizeAny(list);
            }
        }
    }
}