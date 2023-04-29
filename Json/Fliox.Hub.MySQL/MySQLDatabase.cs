// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || MYSQL

using System;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using MySqlConnector;

namespace Friflo.Json.Fliox.Hub.MySQL
{
    public sealed class MySQLDatabase : EntityDatabase
    {
        public              bool            Pretty      { get; init; } = false;
        public              int?            Throughput  { get; init; } = null;
        
        internal readonly   MySqlConnection connection;
        
        public   override   string          StorageType => "MySQL";
        
        public MySQLDatabase(string dbName, MySqlConnection connection, DatabaseService service = null)
            : base(dbName, service)
        {
            this.connection = connection;
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new MySQLContainer(name.AsString(), this, Pretty);
        }
        
    }
    
    internal sealed class MySQLQueryEnumerator : QueryEnumerator
    {
        public   override   JsonKey         Current     => throw new NotImplementedException("not applicable");
        public   override   bool            MoveNext()  => throw new NotImplementedException("not applicable");
        
        // internal MySQLQueryEnumerator(...) {
        // }
        
        protected override void DisposeEnumerator() {
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
