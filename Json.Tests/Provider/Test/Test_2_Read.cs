using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Tests.Provider.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

namespace Friflo.Json.Tests.Provider.Test
{
    /// <summary>
    /// Requires implementation of <see cref="EntityContainer.ReadEntitiesAsync"/>
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class Test_2_Read
    {
        // --- read by id
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_01_One(string db) {
            var client  = await GetClient(db);
            var find    = client.testOps.Read().Find("a-1");
            await client.SyncTasks();
            NotNull(find.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_02_Many(string db) {
            var client  = await GetClient(db);
            var read    = client.testOps.Read();
            var find1   = read.Find("a-1");
            var find2   = read.Find("a-2");
            await client.SyncTasks();
            NotNull(find1.Result);
            NotNull(find2.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_03_Missing(string db) {
            var client  = await GetClient(db);
            var read    = client.testOps.Read();
            var find    = read.Find("unknown");
            await client.SyncTasks();
            IsNull(find.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_04_FieldEscaped(string db) {
            var client  = await GetClient(db);
            var read    = client.testString.Read();
            var quote   = read.Find("s-quote");
            var escape  = read.Find("s-escape");
            await client.SyncTasks();
            NotNull(quote.Result);
            NotNull(escape.Result);
            AreEqual("quote-'",                     quote.Result.str);
            AreEqual("escape-\\-\b-\f-\n-\r-\t-",   escape.Result.str);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_05_IdQuote(string db) {
            var client  = await GetClient(db);
            var read    = client.testString.Read();
            var quote   = read.Find("id-quote-'");
            await client.SyncTasks();
            NotNull(quote.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_06_IdUnicode(string db) {
            var client  = await GetClient(db);
            var read    = client.testString.Read();
            var quote   = read.Find("id-unicode-🔑");
            await client.SyncTasks();
            NotNull(quote.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_07_IntKey_One(string db) {
            var client  = await GetClient(db);
            var read    = client.testIntKey.Read();
            var find    = read.Find(1);
            await client.SyncTasks();
            NotNull(find.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_08_IntKey_Many(string db) {
            var client  = await GetClient(db);
            var read    = client.testIntKey.Read();
            var range   = read.FindRange(new int [] { 1, 2 });
            await client.SyncTasks();
            
            var result = range.Result;
            NotNull(result);
            AreEqual(2, result.Count);
            NotNull(result[1]);
            NotNull(result[2]);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_09_GuidKey_One(string db) {
            var client  = await GetClient(db);
            var read    = client.testGuidKey.Read();
            var find    = read.Find(new Guid("9fa5c8d6-9a24-4562-9861-0c4ffd9ea221"));
            await client.SyncTasks();
            NotNull(find.Result);
        }
        
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_10_GuidKey_Many(string db) {
            Guid guid9f = new Guid("9fa5c8d6-9a24-4562-9861-0c4ffd9ea221");
            Guid guidB3 = new Guid("b36d1650-679c-4ef2-927a-8bdb73e3cfcf");
            var client  = await GetClient(db);
            var read    = client.testGuidKey.Read();
            var range   = read.FindRange(new [] { guid9f, guidB3});
            await client.SyncTasks();
            
            var result = range.Result;
            NotNull(result);
            AreEqual(2, result.Count);
            NotNull(result[guid9f]);
            NotNull(result[guidB3]);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_11_KeyName(string db) {
            var client  = await GetClient(db);
            var read    = client.testKeyName.Read();
            var range   = read.FindRange(new [] { "k-1", "k-2" });
            await client.SyncTasks();
            
            var result = range.Result;
            NotNull(result);
            AreEqual(2, result.Count);
        }
        
        // ------------------------------- test reading common types -------------------------------
        /// <summary>
        /// DateTime format: <see cref="Burst.Bytes.DateTimeFormat"/> 
        /// </summary>
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_20_Guid(string db) {
            var client1 = await GetClient(db);
            var g1      = new TestReadTypes { id = "g1", guid = new Guid("ea8c4fbc-f908-4da5-bf8b-c347dfb62055") };
            client1.testReadTypes.Upsert(g1);
            await client1.SyncTasks();
            
            var client2  = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var g1Read      = read.Find(g1.id);
            await client2.SyncTasks();
            
            AreEqual(g1.guid,       g1Read.Result.guid);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_21_DateTime(string db) {
            var client1  = await GetClient(db);
            var dt1     = new TestReadTypes { id = "dt1", dateTime = DateTime.Parse("2023-07-09 00:00:00Z") };
            var dt2     = new TestReadTypes { id = "dt2", dateTime = DateTime.Parse("2023-07-09 10:00:30.123456Z") };
            var dt3     = new TestReadTypes { id = "dt3", dateTime = DateTime.Parse("2023-07-09 23:59:59.999999Z") };
            client1.testReadTypes.UpsertRange(new [] { dt1, dt2, dt3 });
            await client1.SyncTasks();
            
            var client2  = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var dt1Read = read.Find(dt1.id);
            var dt2Read = read.Find(dt2.id);
            var dt3Read = read.Find(dt3.id);
            await client2.SyncTasks();
            
            AreEqual(dt1.dateTime,   dt1Read.Result.dateTime);
            AreEqual(dt2.dateTime,   dt2Read.Result.dateTime);
            AreEqual(dt3.dateTime,   dt3Read.Result.dateTime);
        }
    }
}