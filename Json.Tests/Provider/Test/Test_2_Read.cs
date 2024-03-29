using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;
using Friflo.Json.Fliox;
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
            var find    = client.testOps.Find("a-1");
            await client.SyncTasksEnv();
            NotNull(find.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_02_Many(string db) {
            var client  = await GetClient(db);
            var read    = client.testOps.Read();
            var find1   = read.Find("a-1");
            var find2   = read.Find("a-2");
            await client.SyncTasksEnv();
            NotNull(find1.Result);
            NotNull(find2.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_03_Missing(string db) {
            var client  = await GetClient(db);
            var read    = client.testOps.Read();
            var find    = read.Find("unknown");
            await client.SyncTasksEnv();
            IsNull(find.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_04_FieldEscaped(string db) {
            var client  = await GetClient(db);
            var read    = client.testString.Read();
            var quote   = read.Find("s-quote");
            var escape  = read.Find("s-escape");
            await client.SyncTasksEnv();
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
            await client.SyncTasksEnv();
            NotNull(quote.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_06_IdUnicode(string db) {
            var client  = await GetClient(db);
            var read    = client.testString.Read();
            var quote   = read.Find("id-unicode-🔑");
            await client.SyncTasksEnv();
            NotNull(quote.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_07_IntKey_One(string db) {
            var client  = await GetClient(db);
            var read    = client.testIntKey.Read();
            var find    = read.Find(1);
            await client.SyncTasksEnv();
            NotNull(find.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_08_IntKey_Many(string db) {
            var client  = await GetClient(db);
            var read    = client.testIntKey.Read();
            var range   = read.FindRange(new int [] { 1, 2 });
            await client.SyncTasksEnv();
            
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
            await client.SyncTasksEnv();
            NotNull(find.Result);
        }
        
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_10_GuidKey_Many(string db) {
            Guid guid9f = new Guid("9fa5c8d6-9a24-4562-9861-0c4ffd9ea221");
            Guid guidB3 = new Guid("b36d1650-679c-4ef2-927a-8bdb73e3cfcf");
            var client  = await GetClient(db);
            var read    = client.testGuidKey.Read();
            var range   = read.FindRange(new [] { guid9f, guidB3});
            await client.SyncTasksEnv();
            
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
            await client.SyncTasksEnv();
            
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
            await client1.SyncTasksEnv();
            
            var client2 = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var g1Read  = read.Find(g1.id);
            await client2.SyncTasksEnv();
            
            AreEqual(g1.guid,       g1Read.Result.guid);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_21_DateTime(string db) {
            var client1 = await GetClient(db);
            var dateTime1 = DateTime.Parse("2023-07-09 00:00:00Z").ToUniversalTime();
            var dateTime2 = DateTime.Parse("2023-07-09 10:00:30.123456Z").ToUniversalTime();
            var dateTime3 = DateTime.Parse("2023-07-09 23:59:59.999999Z").ToUniversalTime();
            
            var dt1     = new TestReadTypes { id = "dt1", dateTime = dateTime1.ToLocalTime() }; // local times are serialized as UTC
            var dt2     = new TestReadTypes { id = "dt2", dateTime = dateTime2 };
            var dt3     = new TestReadTypes { id = "dt3", dateTime = dateTime3 };
            client1.testReadTypes.UpsertRange(new [] { dt1, dt2, dt3 });
            await client1.SyncTasksEnv();
            
            var client2 = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var dt1Read = read.Find(dt1.id);
            var dt2Read = read.Find(dt2.id);
            var dt3Read = read.Find(dt3.id);
            await client2.SyncTasksEnv();
            
            AreEqual(dateTime1,   dt1Read.Result.dateTime);
            AreEqual(dateTime2,   dt2Read.Result.dateTime);
            AreEqual(dateTime3,   dt3Read.Result.dateTime);
            
            AreEqual(DateTimeKind.Utc,   dt1Read.Result.dateTime.Value.Kind);   // deserialized DateTime is always UTC
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_22_IntTypes(string db) {
            var client1 = await GetClient(db);
            var obj     = new ComponentType {
                i64 = 9223372036854775000, // instead long.MaxValue used a value which can also be handled by JS clients
                i32 = int   .MaxValue,
                i16 = short .MaxValue,
                u8  = byte  .MaxValue, // 255
            };
            var int1      = new TestReadTypes { id = "int1", obj = obj };
            client1.testReadTypes.Upsert(int1);
            await client1.SyncTasksEnv();
            
            var client2 = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var int1Read = read.Find(int1.id);
            await client2.SyncTasksEnv();
            
            var result = int1Read.Result.obj;
            AreEqual(obj.i64, result.i64);
            AreEqual(obj.i32, result.i32);
            AreEqual(obj.i16, result.i16);
            AreEqual(obj.u8,  result.u8);
        }
        
        // https://www.hanselman.com/blog/why-you-cant-doubleparsedoublemaxvaluetostring-or-systemoverloadexceptions-when-using-doubleparse
        // [Test]
        public static void TestFloatConversion() {
            {
                double f64 = 1e308;
                var str = f64.ToString(CultureInfo.InvariantCulture);
                var result = double.Parse(str);
                AreEqual(f64, result);
            } {
                float f32 = 1e38f;
                var str = f32.ToString(CultureInfo.InvariantCulture);
                var result = float.Parse(str);
                AreEqual(f32, result);
            }
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_23_FloatTypes(string db) {
            var client1 = await GetClient(db);
            var obj     = new ComponentType {
                f32 = 1e38f,
                f64 = 1e308
            };
            var flt1      = new TestReadTypes { id = "flt1", obj = obj };
            client1.testReadTypes.Upsert(flt1);
            await client1.SyncTasksEnv();
            
            var client2 = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var flt1Read = read.Find(flt1.id);
            await client2.SyncTasksEnv();
            
            var result = flt1Read.Result.obj;
            AreEqual(obj.f32, result.f32);
            AreEqual(obj.f64, result.f64);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_24_IntArray(string db) {
            var client1 = await GetClient(db);
            var i1      = new TestReadTypes { id = "i1", intArray = new [] { 42 } };
            client1.testReadTypes.Upsert(i1);
            await client1.SyncTasksEnv();
            
            var client2 = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var i1Read  = read.Find(i1.id);
            await client2.SyncTasksEnv();
            
            AreEqual(42, i1Read.Result.intArray[0]);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_25_ObjectList(string db) {
            var client1 = await GetClient(db);
            var o1      = new TestReadTypes { id = "o1", objList = new List<ComponentType> { new() { str = "abc" }  } };
            client1.testReadTypes.Upsert(o1);
            await client1.SyncTasksEnv();
            
            var client2 = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var o1Read  = read.Find(o1.id);
            await client2.SyncTasksEnv();
            
            AreEqual("abc", o1Read.Result.objList[0].str);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_26_ClassMember(string db) {
            var client1 = await GetClient(db);
            var c1      = new TestReadTypes { id = "c1", obj = new ComponentType { str = "abc-☀🌎♥👋" } };
            var c2      = new TestReadTypes { id = "c2", obj = new ComponentType() };
            var c3      = new TestReadTypes { id = "c3", obj = null };
            client1.testReadTypes.UpsertRange(new [] { c1, c2, c3 });
            await client1.SyncTasksEnv();
            
            var client2 = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var c1Read  = read.Find(c1.id);
            var c2Read  = read.Find(c2.id);
            var c3Read  = read.Find(c3.id);
            await client2.SyncTasksEnv();
            
            AreEqual("abc-☀🌎♥👋", c1Read.Result.obj.str);
            NotNull (c2Read.Result.obj);
            IsNull  (c3Read.Result.obj);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_27_BigInteger(string db) {
            var client1 = await GetClient(db);
            var bi0     = new TestReadTypes { id = "bi0", bigInt = BigInteger.Zero };
            var bi1     = new TestReadTypes { id = "bi1", bigInt = BigInteger.Parse("1234567890123456789012345678901234567890") };
            client1.testReadTypes.UpsertRange(new [] { bi0, bi1 });
            await client1.SyncTasksEnv();
            
            var client2 = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var bi0Read = read.Find(bi0.id);
            var bi1Read = read.Find(bi1.id);
            await client2.SyncTasksEnv();
            
            AreEqual(bi0.bigInt,   bi0Read.Result.bigInt);
            AreEqual(bi1.bigInt,   bi1Read.Result.bigInt);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_28_ShortString(string db) {
            var client1 = await GetClient(db);
            var ss0     = new TestReadTypes { id = "ss0", shortStr = new ShortString("short-string") };
            client1.testReadTypes.UpsertRange(new [] { ss0 });
            await client1.SyncTasksEnv();
            
            var client2 = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var bi0Read = read.Find(ss0.id);
            await client2.SyncTasksEnv();
            
            IsTrue(ss0.shortStr.IsEqual(bi0Read.Result.shortStr));
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_29_JsonKey(string db) {
            var client1 = await GetClient(db);
            var jk1     = new TestReadTypes { id = "jk1", jsonKey = new JsonKey("json-key") };
            client1.testReadTypes.UpsertRange(new [] { jk1 });
            await client1.SyncTasksEnv();
            
            var client2 = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var bi0Read = read.Find(jk1.id);
            await client2.SyncTasksEnv();
            
            var expected = jk1.jsonValue.AsString();
            var actual   = bi0Read.Result.jsonValue.AsString();
            AreEqual(expected, actual);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_30_JsonValue(string db) {
            var client1 = await GetClient(db);
            var jv1     = new TestReadTypes { id = "jv1", jsonValue = new JsonValue("{\"key\":123}") };
            var jv2     = new TestReadTypes { id = "jv2", jsonValue = new JsonValue("123") };
            var jv3     = new TestReadTypes { id = "jv3", jsonValue = new JsonValue("\"abc\"") };
            var jv4     = new TestReadTypes { id = "jv4", jsonValue = new JsonValue("[10,11]") };
            var jv5     = new TestReadTypes { id = "jv5", jsonValue = new JsonValue("[{\"item-1\":1},{\"item-2\":2}]") };
            var jv6     = new TestReadTypes { id = "jv6", jsonValue = new JsonValue("true") };
            client1.testReadTypes.UpsertRange(new [] { jv1, jv2, jv3, jv4, jv5, jv6 });
            await client1.SyncTasksEnv();
            
            var client2 = await GetClient(db);
            var read    = client2.testReadTypes.Read();
            var jv1Read = read.Find(jv1.id);
            var jv2Read = read.Find(jv2.id);
            var jv3Read = read.Find(jv3.id);
            var jv4Read = read.Find(jv4.id);
            var jv5Read = read.Find(jv5.id);
            var jv6Read = read.Find(jv6.id);
            await client2.SyncTasksEnv();
            
            AreEqualJsonValue(jv1.jsonValue, jv1Read.Result.jsonValue);
            AreEqualJsonValue(jv2.jsonValue, jv2Read.Result.jsonValue);
            AreEqualJsonValue(jv3.jsonValue, jv3Read.Result.jsonValue);
            AreEqualJsonValue(jv4.jsonValue, jv4Read.Result.jsonValue);
            AreEqualJsonValue(jv5.jsonValue, jv5Read.Result.jsonValue);
            AreEqualJsonValue(jv6.jsonValue, jv6Read.Result.jsonValue);
        }
        
        private static void AreEqualJsonValue(JsonValue expected, JsonValue actual) {
            var e   = expected.AsString();
            var a   = actual.AsString();
            AreEqual(e, a);
        }
    }
}