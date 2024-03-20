using System.IO;
using System.Text;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Host
{
    public static class TestMemoryDatabase
    {
        private const string Expect =
@"{
""articles"":
[
{""id"":""article-2"",""name"":""Article 2""},
{""id"":""article-1"",""name"":""Article 1""}
],
""employees"":
[
{""id"":""e-1"",""firstName"":""Peter""}
]
}";
        [Test]
        public static void TestSaveMemoryDatabase() {
            var database    = new MemoryDatabase("test");
            var hub         = new FlioxHub(database);
            var client      = new PocStore(hub);
            client.articles.Create(new Article { id = "article-1", name= "Article 1"});
            client.articles.Create(new Article { id = "article-2", name= "Article 2"});
            client.employees.Create(new Employee { id = "e-1", firstName = "Peter"});
            client.SyncTasksSynchronous();
            
            var stream = new MemoryStream();
            database.WriteToStream(stream);
            stream.Position = 0;
            var dbJson = Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
            Assert.AreEqual(Expect, dbJson);
        }
        
        [Test]
        public static void TestLoadFrom()
        {
            var schema      = DatabaseSchema.Create<PocStore>();
            var database    = new MemoryDatabase("test", schema);
            
            var reader  = new JsonDatabaseReader();
            var json    = new JsonValue(Expect);
            var result  = reader.Read(json, database);
            
            var containers = result.Containers;
            Assert.IsNull(result.error);
            Assert.AreEqual(2, containers.Count);
            Assert.AreEqual(2, containers["articles"]);
            Assert.AreEqual(1, containers["employees"]);
            
            
            var stream = new MemoryStream();
            database.WriteToStream(stream);
            stream.Position = 0;
            var dbJson = Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
            Assert.AreEqual(Expect, dbJson);
        }
    }
}