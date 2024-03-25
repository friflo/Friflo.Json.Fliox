// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || POSTGRESQL

using Friflo.Json.Fliox.Hub.Host.SQL;
using Npgsql;

namespace Friflo.Json.Fliox.Hub.PostgreSQL
{
    internal sealed class SyncConnection : SyncDbConnection
    {
        private readonly   NpgsqlConnection   sqlInstance;
        
        public  override void       ClearPool() => NpgsqlConnection.ClearPool(sqlInstance);
        
        public SyncConnection (NpgsqlConnection instance) : base (instance) {
            sqlInstance = instance;
        }
    }
}

#endif