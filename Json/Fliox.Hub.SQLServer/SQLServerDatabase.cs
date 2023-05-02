// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using Friflo.Json.Fliox.Hub.Host;
using Microsoft.Data.SqlClient;


namespace Friflo.Json.Fliox.Hub.SQLServer
{
    public sealed class SQLServerDatabase : EntityDatabase
    {
        public              bool            Pretty      { get; init; } = false;
        
        internal readonly   SqlConnection   connection;
        
        public   override   string          StorageType => "Microsoft SQL Server";
        
        public SQLServerDatabase(string dbName, SqlConnection connection, DatabaseService service = null)
            : base(dbName, service)
        {
            this.connection = connection;
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new SQLServerContainer(name.AsString(), this, Pretty);
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
