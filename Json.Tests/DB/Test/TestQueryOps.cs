using System.Threading.Tasks;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.DB.Env;
using static System.Math;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Friflo.Json.Tests.DB.Test
{
    public static class TestQueryOps
    {
        private static readonly     int Zero    = 0;
        private static readonly     int One     = 1;
        
        private const int ArticleCount = 2;

        // --- query filter: compare
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Equals(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => One == 1);
            AreEqual("a => 1 == 1", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_NotEquals(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => One != Zero);
            AreEqual("a => 1 != 0", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Less(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => Zero < One);
            AreEqual("a => 0 < 1", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_LessOrEquals(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => Zero <= One);
            AreEqual("a => 0 <= 1", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Greater(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => One > Zero);
            AreEqual("a => 1 > 0", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_GreaterOrEquals(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => One >= Zero);
            AreEqual("a => 1 >= 0", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        // --- query filter: logical
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_And(string db) {
            var store = await GetClient(db);
            bool t = true;
            var query = store.testOps.Query(a => t && true);
            AreEqual("a => true && true", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Or(string db) {
            var store = await GetClient(db);
            bool t = true;
            var query = store.testOps.Query(a => false || t);
            AreEqual("a => false || true", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Not(string db) {
            var store = await GetClient(db);
            bool f = false;
            var query = store.testOps.Query(a => !f);
            AreEqual("a => !(false)", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        // --- query filter: string
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_StartsWith(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => a.id.StartsWith("a-"));
            AreEqual("a => a.id.StartsWith('a-')", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_EndsWith(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => a.id.EndsWith("-1"));
            AreEqual("a => a.id.EndsWith('-1')", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(1, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Contains(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => a.id.Contains('-'));
            AreEqual("a => a.id.Contains('-')", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Contains2(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => a.id.Contains("XXX"));
            AreEqual("a => a.id.Contains('XXX')", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(0, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Length(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => a.id.Length == 3);
            AreEqual("a => a.id.Length() == 3", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        // --- query filter: arithmetic operator
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Add(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => One + One == 2);
            AreEqual("a => 1 + 1 == 2", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Subtract(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => One - One == 0);
            AreEqual("a => 1 - 1 == 0", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Multiply(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => One * One == 1);
            AreEqual("a => 1 * 1 == 1", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Divide(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => One / One == 1);
            AreEqual("a => 1 / 1 == 1", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Modulo(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => One % One == 0);
            AreEqual("a => 1 % 1 == 0", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        // --- query filter: constants
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Constant_E(string db) {
            var store = await GetClient(db);
            double e = 2.718281828459045;
            var query = store.testOps.Query(a => E == e);
            AreEqual("a => 2.718281828459045 == 2.718281828459045", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Constant_Pi(string db) {
            var store = await GetClient(db);
            double pi = 3.141592653589793;
            var query = store.testOps.Query(a => PI == pi);
            AreEqual("a => 3.141592653589793 == 3.141592653589793", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
#if !UNITY_5_3_OR_NEWER
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Constant_Tau(string db) {
            var store = await GetClient(db);
            double tau = 6.283185307179586;
            var query = store.testOps.Query(a => Tau == tau);
            AreEqual("a => 6.283185307179586 == 6.283185307179586", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
#endif
        
        // --- query filter: arithmetic methods
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Abs(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => Abs(-1) == One);
            AreEqual("a => Abs(-1) == 1", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Ceiling(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => Ceiling(1.5) == 2);
            AreEqual("a => Ceiling(1.5) == 2", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Floor(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => Floor(1.5) == 1);
            AreEqual("a => Floor(1.5) == 1", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Exp(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => Exp(1) == 2.718281828459045);
            AreEqual("a => Exp(1) == 2.718281828459045", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Log(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => Log(2.718281828459045) == 1);
            AreEqual("a => Log(2.718281828459045) == 1", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }

        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Sqrt(string db) {
            var store = await GetClient(db);
            var query = store.testOps.Query(a => Sqrt(4) == 2);
            AreEqual("a => Sqrt(4) == 2", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
    }
}