// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System.Data.SqlClient;
using Friflo.Json.Fliox.Hub.Host.SQL;

namespace Friflo.Json.Fliox.Hub.SQLServer
{
    internal sealed class SyncConnection : SyncDbConnection
    {
        internal readonly   SqlConnection   sqlInstance;
        
        public  override void       ClearPool() => SqlConnection.ClearPool(sqlInstance);
        
        public SyncConnection (SqlConnection instance) : base (instance) {
            sqlInstance = instance;
        }
    }
}

#endif