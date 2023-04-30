// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || POSTGRESQL

using System;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Npgsql;


namespace Friflo.Json.Fliox.Hub.PostgreSQL
{
    public sealed class PostgreSQLDatabase : EntityDatabase
    {
        public              bool                Pretty      { get; init; } = false;
        
        internal readonly   NpgsqlConnection    connection;
        
        public   override   string              StorageType => "PostgresQL";
        
        public PostgreSQLDatabase(string dbName, NpgsqlConnection connection, DatabaseService service = null)
            : base(dbName, service)
        {
            this.connection = connection;
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new PostgreSQLContainer(name.AsString(), this, Pretty);
        }
    }
}

namespace System.Runtime.CompilerServices
{
    // This is needed to enable following features in .NET framework and .NET core <= 3.1 projects:
    // - init only setter properties. See [Init only setters - C# 9.0 draft specifications | Microsoft Learn] https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/init
    // - record types
    internal static class IsExternalInit { }
}

#endif
