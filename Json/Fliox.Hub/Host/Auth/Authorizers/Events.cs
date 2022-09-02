using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    // ReSharper disable once CheckNamespace
    public class AuthorizeEvents : Authorizer
    {
        private  readonly   bool          queueEvents;

        public AuthorizeEvents (bool queueEvents) {
            this.queueEvents = queueEvents;
        }
        
        public override void AddAuthorizedDatabases(HashSet<DatabaseFilter> databaseFilters) {
            throw new InvalidOperationException("not a task authorizer");
        }

        public override bool AuthorizeTask(SyncRequestTask task, SyncContext syncContext) {
            throw new InvalidOperationException("not a task authorizer");
        }
        
        public virtual bool AuthorizeRequest (SyncRequest task, SyncContext syncContext) {
            return false;
        }
    }
}