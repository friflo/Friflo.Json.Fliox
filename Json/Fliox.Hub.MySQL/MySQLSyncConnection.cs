// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || MYSQL

using Friflo.Json.Fliox.Hub.Host.SQL;
using MySqlConnector;

namespace Friflo.Json.Fliox.Hub.MySQL
{
    internal sealed class SyncConnection : SyncDbConnection
    {
        private readonly   MySqlConnection   sqlInstance;
        
        public  override void       ClearPool() => MySqlConnection.ClearPool(sqlInstance);
        
        public SyncConnection (MySqlConnection instance) : base (instance) {
            sqlInstance = instance;
        }
    }
}

#endif