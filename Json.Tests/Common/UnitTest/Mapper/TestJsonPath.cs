using System.Collections.Generic;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Graph;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestJsonPath : LeakTestsFixture
    {
        [Test]
        public void TestObjectSelect() {
            using (var typeStore    = new TypeStore()) 
            using (var jsonWriter   = new JsonWriter(typeStore))
            using (var jsonPath     = new JsonPath())
            {
                var sample = new SampleIL();
                var json = jsonWriter.Write(sample);

                var result = jsonPath.Select(json, new [] {
                    ".childStructNull1",
                    ".childStructNull2.val2",
                    ".dbl",
                    ".bln",
                    ".enumIL1",
                    ".child",
                    ".unknown"
                });
                AreEqual(@"{""val2"":68}",  result[0]);
                AreEqual("69",              result[1]);
                AreEqual("94.0",            result[2]);
                AreEqual("true",            result[3]);
                AreEqual(@"""one""",        result[4]);
                AreEqual("null",            result[5]);
                IsNull(                     result[6]);
            }
        }

        public class Chapter
        {
            public string   name;
        }
        
        public class Book
        {
            public string           title;
            public string           author;
            public List<Chapter>    chapters;
        }

        public class Store
        {
            public List<Book>   books;
            
            public void InitSample() {
                books = new List<Book>(new[] {
                    new Book {
                        title = "The Lord of the Rings",
                        author = "J. R. R. Tolkien",
                        chapters = new List<Chapter>() {
                            new Chapter {name = "The Sermon" }
                        }
                    },
                    new Book {
                        title = "Moby Dick",
                        author = "Herman Melville",
                        chapters = new List<Chapter>() {
                            new Chapter { name = "A Long-expected Party"  },
                            new Chapter { name = "The Shadow of the Past" }
                        }
                    }
                });
            }
        }

        [Test]
        public void TestArraySelect() {
            using (var typeStore    = new TypeStore()) 
            using (var jsonWriter   = new JsonWriter(typeStore))
            using (var jsonPath     = new JsonPath())
            {
                var store = new Store();
                store.InitSample();
                var json = jsonWriter.Write(store);
                var selectList = new[] {
                    ".books[*].title",
                    ".books[*].author",
                    ".books[*].chapters[*].name",
                    ".books[*].unknown"
                };
                IList<string> result = new List<string>();
                for (int n = 0; n < 2; n++) {
                    result = jsonPath.Select(json, selectList);
                }
                AssertStoreResult(result);
                
                var selector = new PathSelector(selectList);
                for (int n = 0; n < 2; n++) {
                    jsonPath.Select(json, selector);
                    result = selector.GetResult();
                }
                AssertStoreResult(result);
            }
        }

        private void AssertStoreResult(IList<string> result) {
            AreEqual(@"[""The Lord of the Rings"",""Moby Dick""]",                              result[0]);
            AreEqual(@"[""J. R. R. Tolkien"",""Herman Melville""]",                             result[1]);
            AreEqual(@"[""The Sermon"",""A Long-expected Party"",""The Shadow of the Past""]",  result[2]);
            AreEqual(@"[]",                                                                     result[3]);
        }
    }
}