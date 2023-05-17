using System.Threading.Tasks;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

namespace Friflo.Json.Tests.Provider.Test
{
    // ReSharper disable once InconsistentNaming
    public static class Test_3_6_QueryString
    {
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_Equals_string_Escape(string db) {
            var client  = await GetClient(db);
            var query   = client.testString.Query(c => c.str == "escape-\\-\b-\f-\n-\r-\t-");
            // AreEqual("c => c.str == 'str-1'",      query.filterLinq);
            await client.SyncTasks();
            LogSQL(query.SQL);
            var result =  query.Result;
            AreEqual(1, result.Count);
            AreEqual("s-escape", result[0].id);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_Equals_string_Quote(string db) {
            var client  = await GetClient(db);
            var query   = client.testString.Query(c => c.str == "escape-\\-\b-\f-\n-\r-\t-");
            // AreEqual("c => c.str == 'str-1'",      query.filterLinq);
            await client.SyncTasks();
            LogSQL(query.SQL);
            var result =  query.Result;
            AreEqual(1, result.Count);
            AreEqual("s-escape", result[0].id);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_Equals_string_Unicode(string db) {
            var client  = await GetClient(db);
            var query   = client.testString.Query(c => c.str == "quote-'");
            // AreEqual("c => c.str == 'str-1'",      query.filterLinq);
            await client.SyncTasks();
            LogSQL(query.SQL);
            var result =  query.Result;
            AreEqual(1, result.Count);
            AreEqual("s-quote", result[0].id);
        }
    }
}