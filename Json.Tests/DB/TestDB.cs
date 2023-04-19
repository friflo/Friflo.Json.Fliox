using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Tests.DB.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.DB.Env;
using static System.Math;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Friflo.Json.Tests.DB
{
    public class TestDB
    {
        private static readonly     int Zero    = 0;
        private static readonly     int One     = 1;
        
        [OneTimeSetUp]
        public static void Init() {
            Setup();
        }
        
        private static async Task<TestClient> GetClient(string db) {
            var hub    = await GetDatabaseHub(db);
            return new TestClient(hub);
        }

        private const int ArticleCount = 2;

        // --- query all
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_All(string db) {
            var store = await GetClient(db);
            var query = store.testOps.QueryAll();
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        // --- query filter: compare
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public async Task TestQuery_Equals(string db) {
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
        
        // --- query filter: enum
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_Enum(string db) {
            var store = await GetClient(db);
            var query1 = store.testEnum.Query(t => t.enumVal == TestEnum.e1);
            var query2 = store.testEnum.Query(t => t.enumValNull == TestEnum.e2);
            var query3 = store.testEnum.Query(t => t.enumValNull == null);
            
            var query4 = store.testEnum.Query(t => TestEnum.e1 == t.enumVal);
            var query5 = store.testEnum.Query(t => TestEnum.e2 == t.enumValNull);
            var query6 = store.testEnum.Query(t => null == t.enumValNull);
            
            AreEqual("t => t.enumVal == 'e1'",      query1.DebugQuery.Linq);
            AreEqual("t => t.enumValNull == 'e2'",  query2.DebugQuery.Linq);
            AreEqual("t => t.enumValNull == null",  query3.DebugQuery.Linq);
            
            AreEqual("t => 'e1' == t.enumVal",      query4.DebugQuery.Linq);
            AreEqual("t => 'e2' == t.enumValNull",  query5.DebugQuery.Linq);
            AreEqual("t => null == t.enumValNull",  query6.DebugQuery.Linq);

            await store.SyncTasks();
            
            AreEqual(2, query1.Result.Count);
            AreEqual(1, query2.Result.Count);
            AreEqual(1, query3.Result.Count);
            
            AreEqual(2, query4.Result.Count);
            AreEqual(1, query5.Result.Count);
            AreEqual(1, query6.Result.Count);
        }
        
        // --- query filter: quantify Any
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_AnyIntArray(string db) {
            var store   = await GetClient(db);
            var query   = store.testQuantify.Query(t => t.intArray.Any(i => i == 1));
            AreEqual("t => t.intArray.Any(i => i == 1)",      query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_AnyIntList(string db) {
            var store   = await GetClient(db);
            var query   = store.testQuantify.Query(t => t.intList.Any(i => i == 1));
            AreEqual("t => t.intList.Any(i => i == 1)",      query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_AnyObjectArray(string db) {
            var store   = await GetClient(db);
            var query   = store.testQuantify.Query(t => t.objectArray.Any(o => o.int32 == 10));
            AreEqual("t => t.objectArray.Any(o => o.int32 == 10)",      query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(1, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_AnyObjectList(string db) {
            var store   = await GetClient(db);
            var query   = store.testQuantify.Query(t => t.objectList.Any(o => o.str == "str-10"));
            AreEqual("t => t.objectList.Any(o => o.str == 'str-10')",      query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(1, query.Result.Count);
        }
        
        // --- query filter: quantify All
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_AllIntArray(string db) {
            var store   = await GetClient(db);
            var query   = store.testQuantify.Query(t => t.intArray.All(i => i == 1));
            AreEqual("t => t.intArray.All(i => i == 1)",      query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(4, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_AllIntList(string db) {
            var store   = await GetClient(db);
            var query   = store.testQuantify.Query(t => t.intList.All(i => i == 1));
            AreEqual("t => t.intList.All(i => i == 1)",      query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(4, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_AllObjectArray(string db) {
            var store   = await GetClient(db);
            var query   = store.testQuantify.Query(t => t.objectArray.All(o => o.int32 == 10));
            AreEqual("t => t.objectArray.All(o => o.int32 == 10)",      query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(4, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestQuery_AllObjectList(string db) {
            var store   = await GetClient(db);
            var query   = store.testQuantify.Query(t => t.objectList.All(o => o.str == "str-10"));
            AreEqual("t => t.objectList.All(o => o.str == 'str-10')",      query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(4, query.Result.Count);
        }
        
        // --- read by id
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestRead_One(string db) {
            var store = await GetClient(db);
            var find  = store.testOps.Read().Find("a-1");
            await store.SyncTasks();
            NotNull(find.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestRead_Many(string db) {
            var store = await GetClient(db);
            var read  = store.testOps.Read();
            var find1  = read.Find("a-1");
            var find2  = read.Find("a-2");
            await store.SyncTasks();
            NotNull(find1.Result);
            NotNull(find2.Result);
        }
    }
}
