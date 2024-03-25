// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLITE

using System.Data.Common;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    public sealed class SQLiteConnectionStringBuilder : DbConnectionStringBuilder
    {
        public string DataSource { get => (string)this["Data Source"]; set => this["Data Source"] = value; }
        
        public SQLiteConnectionStringBuilder() { }
        
        public SQLiteConnectionStringBuilder(string connectionString) {
            ConnectionString = connectionString;
        }
    }
}

#endif
