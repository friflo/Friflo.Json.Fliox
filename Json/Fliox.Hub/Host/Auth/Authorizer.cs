// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    /// <summary>
    /// Authorize a given task.
    /// </summary>
    /// <remarks>
    /// This <see cref="Authorizer"/> it stored at <see cref="AuthState.taskAuthorizer"/>.
    /// The <see cref="AuthState.taskAuthorizer"/> is set via <see cref="Authenticator.Authenticate"/> for
    /// <see cref="AuthState.authenticated"/> and for not <see cref="AuthState.authenticated"/> users.
    /// </remarks> 
    public abstract class Authorizer
    {
        /// <summary>
        /// Create a set of <paramref name="databaseFilters"/> used to filter
        /// <see cref="DB.Cluster.ClusterStore"/> read and query results available to a user.
        /// </summary>
        public abstract void    AddAuthorizedDatabases  (HashSet<DatabaseFilter> databaseFilters);
        public abstract bool    AuthorizeTask           (SyncRequestTask task, SyncContext syncContext);
        
        public virtual   bool   AuthorizeRequest        (SyncRequest task, SyncContext syncContext) => throw new InvalidOperationException();
    }
}