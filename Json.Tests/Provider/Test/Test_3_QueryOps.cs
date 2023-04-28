using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
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
    public static class Test_3_QueryOps
    {
        private static readonly     int Zero    = 0;
        private static readonly     int One     = 1;
        
        private const int ArticleCount = 2;

        // --- query filter: compare
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_OpEquals(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => One == 1);
            AreEqual("a => 1 == 1", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_OpNotEquals(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => One != Zero);
            AreEqual("a => 1 != 0", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_OpLess(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => Zero < One);
            AreEqual("a => 0 < 1", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_OpLessOrEquals(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => Zero <= One);
            AreEqual("a => 0 <= 1", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_OpGreater(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => One > Zero);
            AreEqual("a => 1 > 0", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_OpGreaterOrEquals(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => One >= Zero);
            AreEqual("a => 1 >= 0", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        // --- query filter: logical
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_OpAnd(string db) {
            var client  = await GetClient(db);
            bool t      = true;
            var query = client.testOps.Query(a => t && true);
            AreEqual("a => true && true", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_OpOr(string db) {
            var client  = await GetClient(db);
            bool t      = true;
            var query = client.testOps.Query(a => false || t);
            AreEqual("a => false || true", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_OpNot(string db) {
            var client  = await GetClient(db);
            bool f      = false;
            var query = client.testOps.Query(a => !f);
            AreEqual("a => !(false)", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        // --- query filter: string
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_StringStartsWith(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => "start-xxx".StartsWith("start-"));
            AreEqual("a => 'start-xxx'.StartsWith('start-')", query.filterLinq);
            await client.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_StringEndsWith(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => "xxx-end".EndsWith("-end"));
            AreEqual("a => 'xxx-end'.EndsWith('-end')", query.filterLinq);
            await client.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_StringContains(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => "xxx-yyy".Contains('-'));
            AreEqual("a => 'xxx-yyy'.Contains('-')", query.filterLinq);
            await client.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_StringContains2(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => "yyy".Contains("-"));
            AreEqual("a => 'yyy'.Contains('-')", query.filterLinq);
            await client.SyncTasks();
            AreEqual(0, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_StringLength(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => "abc".Length == 3);
            AreEqual("a => 'abc'.Length() == 3", query.filterLinq);
            await client.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        // --- query filter: arithmetic operator
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_OpAdd(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => One + One == 2);
            AreEqual("a => 1 + 1 == 2", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_OpSubtract(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => One - One == 0);
            AreEqual("a => 1 - 1 == 0", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_OpMultiply(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => One * One == 1);
            AreEqual("a => 1 * 1 == 1", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_OpDivide(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => One / One == 1);
            AreEqual("a => 1 / 1 == 1", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_OpModulo(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => One % One == 0);
            AreEqual("a => 1 % 1 == 0", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        // --- query filter: constants
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Constant_E(string db) {
            var client  = await GetClient(db);
            double e = 2.718281828459045;
            var query = client.testOps.Query(a => E == e);
            AreEqual("a => 2.718281828459045 == 2.718281828459045", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Constant_Pi(string db) {
            var client  = await GetClient(db);
            double pi   = 3.141592653589793;
            var query   = client.testOps.Query(a => PI == pi);
            AreEqual("a => 3.141592653589793 == 3.141592653589793", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
#if !UNITY_5_3_OR_NEWER
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Constant_Tau(string db) {
            var client  = await GetClient(db);
            double tau  = 6.283185307179586;
            var query   = client.testOps.Query(a => Tau == tau);
            AreEqual("a => 6.283185307179586 == 6.283185307179586", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
#endif
        
        // --- query filter: arithmetic methods
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_MathAbs(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => Abs(-1) == One);
            AreEqual("a => Abs(-1) == 1", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_MathCeiling(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => Ceiling(1.5) == 2);
            AreEqual("a => Ceiling(1.5) == 2", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_MathFloor(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => Floor(1.5) == 1);
            AreEqual("a => Floor(1.5) == 1", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] // [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_MathExp(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => Exp(1) == 2.718281828459045);
            AreEqual("a => Exp(1) == 2.718281828459045", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] // [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_MathLog(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => Log(2.718281828459045) == 1);
            AreEqual("a => Log(2.718281828459045) == 1", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }

        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] // [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_MathSqrt(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.Query(a => Sqrt(4) == 2);
            AreEqual("a => Sqrt(4) == 2", query.filterLinq);
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
    }
}