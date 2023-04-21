// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using Friflo.Json.Fliox.Hub.Host;
using SQLitePCL;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    public sealed class SQLiteDatabase : EntityDatabase
    {
        public              bool        Pretty      { get; init; } = false;
        public              int?        Throughput  { get; init; } = null;
        
        private  readonly   sqlite3     cosmosDatabase;
        
        public   override   string      StorageType => "CosmosDB";
        
        public SQLiteDatabase(string dbName, DatabaseService service = null)
            : base(dbName, service)
        {
            raw.sqlite3_open("test_db.sqlite3", out cosmosDatabase);
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new SQLiteContainer(name.AsString(), database, Pretty);
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
