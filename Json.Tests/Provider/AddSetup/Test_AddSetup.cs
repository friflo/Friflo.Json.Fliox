using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Tests.Provider.Client;
using NUnit.Framework;
using NUnit.Framework.Constraints;
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
            using var database = CreateDatabase(db, SetupSchema);
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
            await database.DropDatabaseAsync();

            // Check error message of a dropped database
            var hub = new FlioxHub(database);
            var client = new TestClientSetup(hub);
            var read = client.testReadTypes.Read().Find("missing");
            await client.TrySyncTasks();
            
            var kvStorage = IsFileSystem || IsMemoryDB(db);
            if (!kvStorage) {
                IsFalse(read.Success);
                var error = read.Error.Message.Split('\n');
                That(error[0], Does.StartWith("DatabaseError ~ database does not exist:"));
                AreEqual("To create one call: database.SetupDatabaseAsync()", error[1]);
            }
            
            database = CreateDatabase(db, SetupSchema);
            await database.SetupDatabaseAsync();    // 1. create database with only one table and fewer columns 
            
            database = CreateDatabase(db, Schema);
            await database.SetupDatabaseAsync();    // 2. create missing tables and add missing columns 
            // Will create all required columns.
            // The order of columns is different when calling only the 2. SetupDatabaseAsync() without the 1. one
            //
            // Reason: This ensures all INSERT, CREATE and SELECT statements set the column names instead of using *.   

            hub = new FlioxHub(database);
            client = new TestClientSetup(hub);
            var find    = client.testReadTypes.Read().Find("missing");
            await client.SyncTasks();
            
            IsNull(find.Result);
        }
    }
}