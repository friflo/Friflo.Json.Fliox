using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Tests.Provider.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

namespace Friflo.Json.Tests.Provider.AddSetup
{
    /// <summary>
    /// Test <see cref="EntityDatabase.SetupDatabaseAsync"/><br/>
    /// - create a database<br/>
    /// - add columns for <see cref="TableType.Relational"/> database<br/>
    /// - add virtual columns for <see cref="TableType.JsonColumn"/> database<br/>
    /// - create missing tables<br/>
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class Test_AddSetup
    {
        private static readonly DatabaseSchema SetupSchema  = DatabaseSchema.Create<TestClientSetup>();

        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task Test_SetupDatabase(string db) {
            
            var database    = CreateDatabase(db, SetupSchema);
            try {
                await database.DropDatabaseAsync();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception) { } // database already does not exist
            database = CreateDatabase(db, SetupSchema);
            await database.SetupDatabaseAsync();
            
            database = CreateDatabase(db, Schema);
            await database.SetupDatabaseAsync();

            var hub = new FlioxHub(database);
            var client = new TestClientSetup(hub);
            var find    = client.testReadTypes.Read().Find("missing");
            await client.SyncTasks();
            
            IsNull(find.Result);
        }
    }
}