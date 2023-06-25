using System.IO;
using System.Threading.Tasks;
using Lab;
using Friflo.Json.Fliox.Hub.Host;
using NUnit.Framework;

namespace LabTest {

    public static class LabTests
    {
        private static readonly string          DbPath = GetBasePath() + "Lab/Test/DB/main_db";
        private static readonly DatabaseSchema  Schema = DatabaseSchema.Create<LabClient>();

        /// <summary>create a <see cref="MemoryDatabase"/> clone for every client to avoid side effects by DB mutations</summary>
        private static FlioxHub CreateLabHub()
        {
            var cloneDB = CreateMemoryDatabaseClone("main_db", DbPath);
            return new FlioxHub(cloneDB);
        }
        
        private static string GetBasePath()
        {
            string baseDir = Directory.GetCurrentDirectory() + "/../../../../../";
            return Path.GetFullPath(baseDir);
        }
    
        private static MemoryDatabase CreateMemoryDatabaseClone(string dbName, string srcDatabasePath)
        {
            var referenceDB = new FileDatabase("source_db", srcDatabasePath);
            var cloneDB     = new MemoryDatabase(dbName, Schema);
            cloneDB.SeedDatabase(referenceDB).Wait();
            return cloneDB;
        }
        
        [Test]
        public static async Task QueryOrderRelations()
        {
            var hub         = CreateLabHub();
            var client      = new LabClient(hub);
            client.articles.QueryAll();
            await client.SyncTasks();
        }
    }
}