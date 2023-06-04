#if !UNITY_5_3_OR_NEWER

using System;
using Friflo.Json.Fliox.Hub.MySQL;
using Friflo.Json.Fliox.Hub.PostgreSQL;
using Friflo.Json.Fliox.Hub.SQLite;
using Friflo.Json.Fliox.Hub.SQLServer;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable ObjectCreationAsStatement
namespace Friflo.Json.Tests.Provider.Test
{
    public static class TestInit
    {
        [Test]
        public static void TestInit_MissingSchema_SQLite() {
            var e = Throws<ArgumentNullException>(() => { new SQLiteDatabase("test", "empty", null); });
            AreEqual("SQLiteDatabase requires a DatabaseSchema (Parameter 'schema')", e.Message);
        }
        
        [Test]
        public static void TestInit_MissingSchema_MySQL() {
            var e = Throws<ArgumentNullException>(() => { new MySQLDatabase("test", "empty", null); });
            AreEqual("MySQLDatabase requires a DatabaseSchema (Parameter 'schema')", e.Message);
        }
        
        [Test]
        public static void TestInit_MissingSchema_MariaDB() {
            var e = Throws<ArgumentNullException>(() => { new MariaDBDatabase("test", "empty", null); });
            AreEqual("MariaDBDatabase requires a DatabaseSchema (Parameter 'schema')", e.Message);
        }
        
        [Test]
        public static void TestInit_MissingSchema_Postgres() {
            var e = Throws<ArgumentNullException>(() => { new PostgreSQLDatabase("test", "empty", null); });
            AreEqual("PostgreSQLDatabase requires a DatabaseSchema (Parameter 'schema')", e.Message);
        }
        
        [Test]
        public static void TestInit_MissingSchema_SQLServer() {
            var e = Throws<ArgumentNullException>(() => { new SQLServerDatabase("test", "empty", null); });
            AreEqual("SQLServerDatabase requires a DatabaseSchema (Parameter 'schema')", e.Message);
        }
    }
}

#endif
