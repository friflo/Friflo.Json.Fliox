// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    /// <summary>
    /// Authorize a given task.
    /// </summary>
    /// <remarks>
    /// This <see cref="TaskAuthorizer"/> it stored at <see cref="AuthState.taskAuthorizer"/>.
    /// The <see cref="AuthState.taskAuthorizer"/> is set via <see cref="Authenticator.Authenticate"/> for
    /// <see cref="AuthState.authenticated"/> and for not <see cref="AuthState.authenticated"/> users.
    /// </remarks> 
    public abstract class TaskAuthorizer
    {
        /// <summary>
        /// Create a set of <paramref name="databaseFilters"/> used to filter
        /// <see cref="DB.Cluster.ClusterStore"/> read and query results available to a user.
        /// </summary>
        public abstract void    AddAuthorizedDatabases  (HashSet<DatabaseFilter> databaseFilters);
        public abstract bool    AuthorizeTask           (SyncRequestTask task, SyncContext syncContext);
        
        internal static TaskAuthorizer ToAuthorizer(IReadOnlyList<TaskAuthorizer> authorizers) {
            if (authorizers == null)
                return AuthorizeDeny.Instance;
            if (authorizers.Count == 0)
                return AuthorizeDeny.Instance;
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