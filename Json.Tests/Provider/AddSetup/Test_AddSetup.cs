using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Tests.Provider.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Tests.Provider.AddSetup
{
    /// <summary>
    /// Test <see cref="EntityDatabase.SetupDatabaseAsync"/><br/>
    /// - create a database<br/>
    /// - add columns for <see cref="TableType.Relational"/> database<br/>
    /// - add virtual columns for <see cref="TableType.JsonColumn"/> database<br/>
    /// - create missing tables<br/>
    /// </summary>
    // [Ignore("to save a second for each database test")]
    public static class Test_AddSetup
    {
        private static readonly DatabaseSchema SetupSchema  = DatabaseSchema.Create<TestClientSetup>();

        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task Test_1_DropContainers(string db)
        {
            if (IsFileSystem) return; // don't delete test data
            var database = CreateDatabase(db, SetupSchema);
            await database.SetupDatabaseAsync();        // will create missing tables
            await database.DropAllContainersAsync();    // drop all tables
            await database.SetupDatabaseAsync();        // will create all tables
            
            var hub = new FlioxHub(database);
            var client = new TestClientSetup(hub);
            var find    = client.testReadTypes.Read().Find("missing");
            await client.SyncTasks();
            
            IsNull(find.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task Test_2_SetupDatabase(string db)
        {
            if (IsFileSystem) return; // don't delete test data
            var database = CreateDatabase(db, Schema);
            try {
                await database.DropDatabaseAsync();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception) {
                // database already does not exist   
            }
            database = CreateDatabase(db, SetupSchema);
            await database.SetupDatabaseAsync();    // create database with only one table and fewer columns 
            
            database = CreateDatabase(db, Schema);
            await database.SetupDatabaseAsync();    // create missing tables and add missing columns 

            var hub = new FlioxHub(database);
            var client = new TestClientSetup(hub);
            var find    = client.testReadTypes.Read().Find("missing");
            await client.SyncTasks();
            
            IsNull(find.Result);
        }
    }
}