using System.Threading.Tasks;
using Friflo.Playground.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Playground.DB.Env;
using static System.Math;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Friflo.Playground.DB
{
    public class TestDB
    {
        private readonly     int Zero    = 0;
        private readonly     int One     = 1;
        
        [OneTimeSetUp]
        public static void Init() {
            Setup();
        }
        
        private static TestClient GetClient(string db) {
            var hub    = GetDatabaseHub(db);
            return new TestClient(hub);
        }

        private const int ArticleCount = 2;

        // --- query all
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_All(string db) {
            var store = GetClient(db);
            var query = store.articles.QueryAll();
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        // --- query filter: compare
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public async Task TestQuery_Equals(string db) {
            var store = GetClient(db);
            int one = 1;
            // ReSharper disable once EqualExpressionComparison
            var query = store.articles.Query(a => one == one);
            AreEqual("a => 1 == 1", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_NotEquals(string db) {
            var store = GetClient(db);
            int zero = 0; int one = 1; 
            var query = store.articles.Query(a => one != zero);
            AreEqual("a => 1 != 0", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Less(string db) {
            var store = GetClient(db);
            int zero = 0; int one = 1; 
            var query = store.articles.Query(a => zero < one);
            AreEqual("a => 0 < 1", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_LessOrEquals(string db) {
            var store = GetClient(db);
            int zero = 0; int one = 1; 
            var query = store.articles.Query(a => zero <= one);
            AreEqual("a => 0 <= 1", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Greater(string db) {
            var store = GetClient(db);
            int zero = 0; int one = 1; 
            var query = store.articles.Query(a => one > zero);
            AreEqual("a => 1 > 0", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_GreaterOrEquals(string db) {
            var store = GetClient(db);
            int zero = 0; int one = 1; 
            var query = store.articles.Query(a => one >= zero);
            AreEqual("a => 1 >= 0", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        // --- query filter: logical
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_And(string db) {
            var store = GetClient(db);
            bool t = true;
            // ReSharper disable once RedundantLogicalConditionalExpressionOperand
            var query = store.articles.Query(a => t && true);
            AreEqual("a => true && true", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Or(string db) {
            var store = GetClient(db);
            bool t = true;
            // ReSharper disable once RedundantLogicalConditionalExpressionOperand
            var query = store.articles.Query(a => false || t);
            AreEqual("a => false || true", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Not(string db) {
            var store = GetClient(db);
            bool f = false;
            var query = store.articles.Query(a => !f);
            AreEqual("a => !(false)", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        // --- query filter: string
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_StartsWith(string db) {
            var store = GetClient(db);
            var query = store.articles.Query(a => a.id.StartsWith("a-"));
            AreEqual("a => a.id.StartsWith('a-')", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_EndsWith(string db) {
            var store = GetClient(db);
            var query = store.articles.Query(a => a.id.EndsWith("-1"));
            AreEqual("a => a.id.EndsWith('-1')", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(1, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Contains(string db) {
            var store = GetClient(db);
            var query = store.articles.Query(a => a.id.Contains("-"));
            AreEqual("a => a.id.Contains('-')", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Contains2(string db) {
            var store = GetClient(db);
            var query = store.articles.Query(a => a.id.Contains("XXX"));
            AreEqual("a => a.id.Contains('XXX')", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(0, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Length(string db) {
            var store = GetClient(db);
            var query = store.articles.Query(a => a.id.Length == 3);
            AreEqual("a => a.id.Length() == 3", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        // --- query filter: arithmetic operator
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Add(string db) {
            var store = GetClient(db);
            int one = 1;
            var query = store.articles.Query(a => one + one == 2);
            AreEqual("a => 1 + 1 == 2", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Subtract(string db) {
            var store = GetClient(db);
            int one = 1;
            var query = store.articles.Query(a => one - one == 0);
            AreEqual("a => 1 - 1 == 0", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Multiply(string db) {
            var store = GetClient(db);
            int one = 1;
            var query = store.articles.Query(a => one * one == 1);
            AreEqual("a => 1 * 1 == 1", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Divide(string db) {
            var store = GetClient(db);
            int one = 1;
            var query = store.articles.Query(a => one / one == 1);
            AreEqual("a => 1 / 1 == 1", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Modulo(string db) {
            var store = GetClient(db);
            int one = 1;
            var query = store.articles.Query(a => one % one == 0);
            AreEqual("a => 1 % 1 == 0", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        // --- query filter: constants
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Constant_E(string db) {
            var store = GetClient(db);
            double e = 2.718281828459045;
            var query = store.articles.Query(a => E == e);
            AreEqual("a => 2.718281828459045 == 2.718281828459045", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Constant_Pi(string db) {
            var store = GetClient(db);
            double pi = 3.141592653589793;
            var query = store.articles.Query(a => PI == pi);
            AreEqual("a => 3.141592653589793 == 3.141592653589793", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
#if !UNITY_5_3_OR_NEWER
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Constant_Tau(string db) {
            var store = GetClient(db);
            double tau = 6.283185307179586;
            var query = store.articles.Query(a => Tau == tau);
            AreEqual("a => 6.283185307179586 == 6.283185307179586", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
#endif
        
        // --- query filter: arithmetic methods
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Abs(string db) {
            var store = GetClient(db);
            int one = 1;
            var query = store.articles.Query(a => Abs(-1) == one);
            AreEqual("a => Abs(-1) == 1", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Ceiling(string db) {
            var store = GetClient(db);
            var query = store.articles.Query(a => Ceiling(1.5) == 2);
            AreEqual("a => Ceiling(1.5) == 2", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Floor(string db) {
            var store = GetClient(db);
            var query = store.articles.Query(a => Floor(1.5) == 1);
            AreEqual("a => Floor(1.5) == 1", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Exp(string db) {
            var store = GetClient(db);
            var query = store.articles.Query(a => Exp(1) == 2.718281828459045);
            AreEqual("a => Exp(1) == 2.718281828459045", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Log(string db) {
            var store = GetClient(db);
            var query = store.articles.Query(a => Log(2.718281828459045) == 1);
            AreEqual("a => Log(2.718281828459045) == 1", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }

        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQuery_Sqrt(string db) {
            var store = GetClient(db);
            var query = store.articles.Query(a => Sqrt(4) == 2);
            AreEqual("a => Sqrt(4) == 2", query.DebugQuery.Linq);
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }
        
        // --- read by id
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestRead_One(string db) {
            var store = GetClient(db);
            var find  = store.articles.Read().Find("a-1");
            await store.SyncTasks();
            NotNull(find.Result);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestRead_Many(string db) {
            var store = GetClient(db);
            var read  = store.articles.Read();
            var find1  = read.Find("a-1");
            var find2  = read.Find("a-2");
            await store.SyncTasks();
            NotNull(find1.Result);
            NotNull(find2.Result);
        }
    }
}
