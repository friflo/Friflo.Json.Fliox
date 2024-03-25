// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs
{
    public class NoopDatabaseHub : FlioxHub
    {
        internal NoopDatabaseHub (string databaseName, SharedEnv env)
            : base(new NoopDatabase(databaseName), env)
        { }
                
        public override Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext) {
            var result  = new SyncResponse {
                tasks   = new ListOne<SyncTaskResult>()
            };
            var response = new ExecuteSyncResult(result);
            return Task.FromResult(response);
        }
    }
    
    internal class NoopDatabase : EntityDatabase
    {
        public   override   string      StorageType => "Noop";
        
        internal NoopDatabase(string dbName) : base(dbName, null, null) { }

        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            throw new InvalidOperationException("NoopDatabase cannot create a container");
        }
    }
}