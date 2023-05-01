using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

namespace Friflo.Json.Tests.Provider.Test
{
    /// <summary>
    /// Requires implementation of cursors used in <see cref="EntityContainer.QueryEntitiesAsync"/>.<br/>
    /// The cursor is available in the <c>command</c> parameter with <see cref="QueryEntities.cursor"/>.  
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class Test_5_QueryCursor
    {
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Limit(string db) {
            var client  = await GetClient(db);
            var query   = client.testCursor.QueryAll();
            query.limit = 2;
            await client.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        // Using maxCount less than available entities. So multiple query are required to return all entities.
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Cursor_MultiStep(string db) {
            var client      = await GetClient(db);
            var query       = client.testCursor.QueryAll();
            int count       = 0;
            int iterations  = 0;
            while (true) {
                query.maxCount  = 2;    // query with cursor
                iterations++;
                await client.SyncTasks();
                
                count          += query.Result.Count;
                var cursor      = query.ResultCursor;
                if (cursor == null)
                    break;
                query           = client.testCursor.QueryAll();
                query.cursor    = cursor;
            }
            AreEqual(3, iterations);
            AreEqual(5, count);
        }
        
        // Using maxCount greater than available entities. So a single query return all entities.
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Cursor_SingleStep(string db) {
            var client      = await GetClient(db);
            var query       = client.testCursor.QueryAll();
            query.maxCount  = 100;      // query with cursor
            await client.SyncTasks();
                
            AreEqual(5, query.Result.Count);
            IsNull(query.ResultCursor);
        }
        
        // Using maxCount less than available entities matching the filter.
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Cursor_Filter(string db) {
            var client      = await GetClient(db);
            var query       = client.testCursor.Query(c => c.value == 100);
            int count       = 0;
            int iterations  = 0;
            while (true) {
                query.maxCount  = 2;    // query with cursor
                iterations++;
                await client.SyncTasks();
                
                count          += query.Result.Count;
                var cursor      = query.ResultCursor;
                if (cursor == null)
                    break;
                query           = client.testCursor.Query(c => c.value == 100); // todo add QueryNext()
                query.cursor    = cursor;
            }
            AreEqual(2, iterations);
            AreEqual(3, count);
        }
        
        // Close a specific cursor manually
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Cursor_Close(string db) {
            // cursors (Continuation tokens) in CosmosDB are stateless => no support for closing implemented / required.
            // See [Pagination in Azure Cosmos DB | Microsoft Learn | Continuation tokens]
            //     https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/query/pagination#continuation-tokens
            if (IsCosmosDB || IsPostgres || IsMySQL || IsMariaDB) return;
            var client          = await GetClient(db);
            var query1          = client.testCursor.QueryAll();
            query1.maxCount     = 2;    // query with cursor
            var closeCursors1   = client.testCursor.CloseCursors(Array.Empty<string>());
            await client.SyncTasks();
            
            AreEqual(2, query1.Result.Count);
            AreEqual(1, closeCursors1.Count);
            
            var closeCursors2   = client.testCursor.CloseCursors(new[] { query1.ResultCursor });
            var query2          = client.testCursor.QueryAll(); // todo add QueryNext()
            query2.cursor       = query1.ResultCursor;
            await client.TrySyncTasks();
            
            IsFalse(query2.Success);    // cursor was closed
            AreEqual(0, closeCursors2.Count);
        }
        
        // Close all cursors manually
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Cursor_CloseAll(string db) {
            if (IsCosmosDB || IsPostgres || IsMySQL || IsMariaDB) return; // see comment above
            var client          = await GetClient(db);
            var query1          = client.testCursor.QueryAll();
            query1.maxCount     = 2;    // query with cursor
            var closeCursors1   = client.testCursor.CloseCursors(Array.Empty<string>());
            await client.SyncTasks();
            
            AreEqual(2, query1.Result.Count);
            AreEqual(1, closeCursors1.Count);

            var closeCursors2   = client.testCursor.CloseCursors(null);
            var query2          = client.testCursor.QueryAll(); // todo add QueryNext()
            query2.cursor       = query1.ResultCursor;
            await client.TrySyncTasks();
            
            IsFalse(query2.Success);    // cursor was closed
            AreEqual(0, closeCursors2.Count);
        }
        
        // Close unknown cursors manually
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Cursor_CloseNullUnknown(string db) {
            var client          = await GetClient(db);
            var closeCursors    = client.testCursor.CloseCursors(new[] { null, "unknown-cursor" });
            await client.SyncTasks();
            
            IsTrue(closeCursors.Success);
            AreEqual(0, closeCursors.Count);
        }
        
    }
}
