// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using Friflo.Json.Fliox.Hub.SQLite;
using NUnit.Framework;
using static NUnit.Framework.Assert;


// ReSharper disable UseObjectOrCollectionInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.SQLite
{
    public static class TestSQLite
    {
        [Test]
        public static void TestSQLiteConnectionString() {
            var cs = new SQLiteConnectionStringBuilder("Data Source=test-1.sqlite3");
            AreEqual("test-1.sqlite3", cs.DataSource);
            
            cs = new SQLiteConnectionStringBuilder();
            cs.DataSource = "test-2.sqlite3";

            AreEqual("Data Source=test-2.sqlite3", cs.ConnectionString);
        }
    }
}

#endif
