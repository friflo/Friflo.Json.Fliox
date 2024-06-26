using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Cosmos;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Tests.Provider.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;
using static System.Math;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Friflo.Json.Tests.Provider.Test
{
    /// <summary>
    /// Requires implementation of the filter conversion required in <see cref="EntityContainer.QueryEntitiesAsync"/>.<br/>
    /// This filter is available in the <c>command</c> parameter with <see cref="QueryEntities.GetFilter"/>.  
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class Test_3_2_QueryFields
    {
        private const int All = 4;
        
        // --- query filter: enum
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Enum(string db) {
            var client  = await GetClient(db);
            var query1  = client.testEnum.Query(t => t.enumVal == TestEnum.e1);
            var query2  = client.testEnum.Query(t => t.enumValNull == TestEnum.e2);
            var query3  = client.testEnum.Query(t => t.enumValNull == null);
            
            var query4  = client.testEnum.Query(t => TestEnum.e1 == t.enumVal);
            var query5  = client.testEnum.Query(t => TestEnum.e2 == t.enumValNull);
            var query6  = client.testEnum.Query(t => null == t.enumValNull);
            
            AreEqual("t => t.enumVal == 'e1'",      query1.filterLinq);
            AreEqual("t => t.enumValNull == 'e2'",  query2.filterLinq);
            AreEqual("t => t.enumValNull == null",  query3.filterLinq);
            
            AreEqual("t => 'e1' == t.enumVal",      query4.filterLinq);
            AreEqual("t => 'e2' == t.enumValNull",  query5.filterLinq);
            AreEqual("t => null == t.enumValNull",  query6.filterLinq);

            await client.SyncTasksEnv();
            
            AreEqual(3, query1.Result.Count);
            AreEqual(1, query2.Result.Count);
            AreEqual(2, query3.Result.Count);
            
            AreEqual(3, query4.Result.Count);
            AreEqual(1, query5.Result.Count);
            AreEqual(2, query6.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_Equals_number(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Query(c => c.int32 == 1);
            AreEqual("c => c.int32 == 1",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(1, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_Equals_string(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Query(c => c.str == "str-1");
            AreEqual("c => c.str == 'str-1'",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(1, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_Equals_true(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Query(c => c.boolean == true);
            AreEqual("c => c.boolean == true",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(1, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_Equals_false(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Query(c => c.boolean == false);
            AreEqual("c => c.boolean == false",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(1, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_Equals_null(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Query(c => c.int32 == null);
            AreEqual("c => c.int32 == null",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(All - 2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_Equals_null2(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Query(c => null ==  c.int32);
            AreEqual("c => null == c.int32",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(All - 2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_NotEquals_null(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Query(c => c.int32 != null);
            AreEqual("c => c.int32 != null",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(2, query.Result.Count);
        }
        
        /// <summary>Apply LINQ behavior. <see cref="TestQuery_Compare_NotEquals_Reference"/></summary>
        /// <param name="db"></param>
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_NotEquals_Number(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Query(c => c.int32 != 1);
            AreEqual("c => c.int32 != 1",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(All - 1, query.Result.Count);
        }
        
        /// <summary>Apply LINQ behavior. <see cref="TestQuery_Compare_NotEquals_Reference"/></summary>
        /// <param name="db"></param>
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_NotEquals_String(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Query(c => c.str != "str-0");
            AreEqual("c => c.str != 'str-0'",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(All - 1, query.Result.Count);
        }
        
        /// <summary>
        /// As reference:<br/>
        /// Applying LINQ Where() and Select() behavior using operator != on null values.<br/>
        /// null != 1 evaluates to true<br/>
        /// <br/>
        /// It also fits to the expectation of (i => i != 1) and (i => i == 1)<br/>
        /// 1. Their intersection is an empty set.<br/>
        /// 2. Their union is the set of all elements.<br/><br/>
        /// <br/>
        /// The alternative would be always omitting null elements but this this would violate expectation 2. 
        /// </summary>
        [Test]
        public static void TestQuery_Compare_NotEquals_Reference() {
            var list = new List<int?> { 1, null, 2 };
            var result = list.Where(item => item != 1).ToArray();
            AreEqual(2, result.Length);
            AreEqual(null, result[0]);
            AreEqual(2,    result[1]);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_Less_Int(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Query(c => c.int32 < 2);
            AreEqual("c => c.int32 < 2",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_LessEqual_Int(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Query(c => c.int32 <= 1);
            AreEqual("c => c.int32 <= 1",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_Greater_Int(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Query(c => c.int32 > 0);
            AreEqual("c => c.int32 > 0",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(1, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_GreaterEqual_Int(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Query(c => c.int32 >= 0);
            AreEqual("c => c.int32 >= 0",       query.filterLinq);
            AreEqual("c['int32'] >= 0",         query.filter.CosmosFilter());
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_String_Length(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Query(c => c.str.Length == 5);
            AreEqual("c => c.str.Length() == 5", query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(2, query.Result.Count);
        }
        
        // --- query filter: arithmetic operator
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_CompareAdd(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Query(c => Abs(c.int32.Value) > 0);
            AreEqual("c => Abs(c.int32) > 0", query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(1, query.Result.Count);
        }
    }
}
