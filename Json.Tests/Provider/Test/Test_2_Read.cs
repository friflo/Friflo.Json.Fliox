using System;
using System.Collections.Generic;
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
        /// <summary> DateTime format: <see cref="Burst.Bytes.DateTimeFormat"/></summary>
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_20_Guid(string db) {
            var client1 = await GetClient(db);
            var g1      = new TestReadTypes { id = "g1", guid = new Guid("ea8c4fbc-f908-4da5-bf8b-c347dfb62055") };
            client1.testReadTypes.Upsert(g1);
            await client1.SyncTasks();
            
            var client2 = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var g1Read  = read.Find(g1.id);
            await client2.SyncTasks();
            
            AreEqual(g1.guid,       g1Read.Result.guid);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_21_DateTime(string db) {
            var client1 = await GetClient(db);
            var dt1     = new TestReadTypes { id = "dt1", dateTime = DateTime.Parse("2023-07-09 00:00:00Z") };
            var dt2     = new TestReadTypes { id = "dt2", dateTime = DateTime.Parse("2023-07-09 10:00:30.123456Z") };
            var dt3     = new TestReadTypes { id = "dt3", dateTime = DateTime.Parse("2023-07-09 23:59:59.999999Z") };
            client1.testReadTypes.UpsertRange(new [] { dt1, dt2, dt3 });
            await client1.SyncTasks();
            
            var client2 = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var dt1Read = read.Find(dt1.id);
            var dt2Read = read.Find(dt2.id);
            var dt3Read = read.Find(dt3.id);
            await client2.SyncTasks();
            
            AreEqual(dt1.dateTime,   dt1Read.Result.dateTime);
            AreEqual(dt2.dateTime,   dt2Read.Result.dateTime);
            AreEqual(dt3.dateTime,   dt3Read.Result.dateTime);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_22_IntTypes(string db) {
            var client1 = await GetClient(db);
            var obj     = new ComponentType {
                i64 = long  .MaxValue,
                i32 = int   .MaxValue,
                i16 = short .MaxValue,
                u8  = byte  .MaxValue, // 255
            };
            var int1      = new TestReadTypes { id = "int1", obj = obj };
            client1.testReadTypes.Upsert(int1);
            await client1.SyncTasks();
            
            var client2 = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var int1Read = read.Find(int1.id);
            await client2.SyncTasks();
            
            var result = int1Read.Result.obj;
            AreEqual(obj.i64, result.i64);
            AreEqual(obj.i32, result.i32);
            AreEqual(obj.i16, result.i16);
            AreEqual(obj.u8,  result.u8);
        }
        
        // [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_23_FloatTypes(string db) {
            var client1 = await GetClient(db);
            var obj     = new ComponentType {
                f64 = double.MaxValue,
                f32 = float .MaxValue
            };
            var flt1      = new TestReadTypes { id = "flt1", obj = obj };
            client1.testReadTypes.Upsert(flt1);
            await client1.SyncTasks();
            
            var client2 = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var flt1Read = read.Find(flt1.id);
            await client2.SyncTasks();
            
            var result = flt1Read.Result.obj;
            AreEqual(obj.f64, result.f64);
            AreEqual(obj.f32, result.f32);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_24_IntArray(string db) {
            var client1 = await GetClient(db);
            var i1      = new TestReadTypes { id = "i1", intArray = new [] { 42 } };
            client1.testReadTypes.Upsert(i1);
            await client1.SyncTasks();
            
            var client2 = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var i1Read  = read.Find(i1.id);
            await client2.SyncTasks();
            
            AreEqual(42, i1Read.Result.intArray[0]);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_25_ObjectList(string db) {
            var client1 = await GetClient(db);
            var o1      = new TestReadTypes { id = "o1", objList = new List<ComponentType> { new() { str = "abc" }  } };
            client1.testReadTypes.Upsert(o1);
            await client1.SyncTasks();
            
            var client2 = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var o1Read  = read.Find(o1.id);
            await client2.SyncTasks();
            
            AreEqual("abc", o1Read.Result.objList[0].str);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_26_ClassMember(string db) {
            var client1 = await GetClient(db);
            var c1      = new TestReadTypes { id = "c1", obj = new ComponentType { str = "abc-☀🌎♥👋" } };
            var c2      = new TestReadTypes { id = "c2", obj = null };
            client1.testReadTypes.UpsertRange(new [] { c1, c2 });
            await client1.SyncTasks();
            
            var client2 = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var c1Read  = read.Find(c1.id);
            var c2Read  = read.Find(c2.id);
            await client2.SyncTasks();
            
            AreEqual("abc-☀🌎♥👋", c1Read.Result.obj.str);
            IsNull(c2Read.Result.obj);
        }
    }
}