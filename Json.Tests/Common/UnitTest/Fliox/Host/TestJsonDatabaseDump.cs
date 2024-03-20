using System.IO;
using System.Text;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Host
{
    public static class TestJsonDatabaseDump
    {
        private const string Expect =
@"{
""articles"":
[
{""id"":""article-1"",""name"":""Article 1""},
{""id"":""article-2"",""name"":""Article 2""}
],
""employees"":
[
{""id"":""e-1"",""firstName"":""Peter""}
]
}";
        [Test]
        public static void TestSaveMemoryDatabase()
        {
            var database    = new MemoryDatabase("test") { ContainerType = MemoryType.NonConcurrent };
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
            var database    = new MemoryDatabase("test", schema) { ContainerType = MemoryType.NonConcurrent };
            
            var reader  = new JsonDatabaseDumpReader();
            var json    = new JsonValue(Expect);
            var result  = reader.Read(json, database);
            
            var containers = result.containers;
            Assert.IsNull  (   result.error);
            Assert.AreEqual(3, result.EntityCount);
            Assert.AreEqual(2, containers.Count);
            Assert.AreEqual(2, containers["articles"]);
            Assert.AreEqual(1, containers["employees"]);
            Assert.AreEqual("entities: 3", result.ToString());
            
            var stream = new MemoryStream();
            database.WriteToStream(stream);
            stream.Position = 0;
            var dbJson = Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
            Assert.AreEqual(Expect, dbJson);
        }
        
        [Test]
        public static void TestLoadErrors()
        {
            var schema      = DatabaseSchema.Create<PocStore>();
            var database    = new MemoryDatabase("test", schema) { ContainerType = MemoryType.NonConcurrent };
            var reader      = new JsonDatabaseDumpReader();
            {
                var json    = new JsonValue("[");
                var result  = reader.Read(json, database);
                Assert.AreEqual("entities: 0 - error: expect object. was: ArrayStart at position: 1", result.ToString());
            } {
                var json    = new JsonValue("x");
                var result  = reader.Read(json, database);
                Assert.AreEqual("entities: 0 - error: unexpected character while reading value. Found: x path: '(root)' at position: 1", result.ToString());
            } {
                var json    = new JsonValue("{x");
                var result  = reader.Read(json, database);
                Assert.AreEqual("entities: 0 - error: unexpected character > expect key. Found: x path: '(root)' at position: 2", result.ToString());
            } {
                var json    = new JsonValue("{\"c1\": {");
                var result  = reader.Read(json, database);
                Assert.AreEqual("entities: 0 - error: expect array. was: ObjectStart at position: 8", result.ToString());
            } {
                var json    = new JsonValue("{\"unknown\": [");
                var result  = reader.Read(json, database);
                Assert.AreEqual("entities: 0 - error: container not found. was: 'unknown' at position: 13", result.ToString());
            } {
                var json    = new JsonValue("{\"articles\": [1 ");
                var result  = reader.Read(json, database);
                Assert.AreEqual("entities: 0 - error: expect object. was: ValueNumber at position: 15", result.ToString());
            } {
                var json    = new JsonValue("{\"articles\": [x ");
                var result  = reader.Read(json, database);
                Assert.AreEqual("entities: 0 - error: unexpected character while reading value. Found: x path: 'articles[0]' at position: 15", result.ToString());
            }
        }
    }
}