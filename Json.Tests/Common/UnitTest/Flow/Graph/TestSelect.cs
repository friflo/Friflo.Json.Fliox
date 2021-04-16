using System.Collections.Generic;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Graph.Select;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Tests.Common.UnitTest.Flow.Mapper;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class TestSelect : LeakTestsFixture
    {
        [Test]
        public void TestObjectSelect() {
            using (var typeStore    = new TypeStore()) 
            using (var jsonWriter   = new ObjectWriter(typeStore))
            using (var jsonSelector = new JsonSelector())
            {
                var sample = new SampleIL();
                var json = jsonWriter.Write(sample);

                var select = new JsonSelect(new[] {
                    ".childStructNull1",
                    ".childStructNull2.val2",
                    ".dbl",
                    ".bln",
                    ".enumIL1",
                    ".child",
                    ".unknown"
                });
                var result = jsonSelector.Select(json, select);
                
                // AreEqual(@"[{""val2"":68}]",    result[0].ToString());
                AreEqual("[69]",                result[1].ToString());
                AreEqual("[94]",                result[2].ToString());
                AreEqual("[true]",              result[3].ToString());
                AreEqual("['one']",             result[4].ToString());
                AreEqual("[null]",              result[5].ToString());
                AreEqual("[]",                  result[6].ToString());
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
            using (var jsonWriter   = new ObjectWriter(typeStore))
            using (var jsonSelector = new JsonSelector())
            {
                var store = new Store();
                store.InitSample();
                var json = jsonWriter.Write(store);
                var select = new JsonSelect(new[] {
                    ".books[*].title",
                    ".books[*].author",
                    ".books[*].chapters[*].name",
                    ".books[*].unknown"
                });
                var result = new List<ScalarResult>();
                for (int n = 0; n < 2; n++) {
                    result = jsonSelector.Select(json, select);
                }
                AssertStoreResult(result);
                
                for (int n = 0; n < 2; n++) {
                     jsonSelector.Select(json, select);
                     result = select.Results; // alternative access to results
                }
                AssertStoreResult(result);
            }
        }

        private void AssertStoreResult(List<ScalarResult> result) {
            AreEqual("['The Lord of the Rings','Moby Dick']",                           result[0].ToString());
            AreEqual("['J. R. R. Tolkien','Herman Melville']",                          result[1].ToString());
            AreEqual("['The Sermon','A Long-expected Party','The Shadow of the Past']", result[2].ToString());
            AreEqual("[]",                                                              result[3].ToString());
        }

        [Test]
        public void TestArrayGroupSelect() {
            var selectors = new[] {
                ".children[=>].hobbies[*].name", // group by using [=>]
                ".children[*].hobbies[*].name"   // dont group by using [*]
            };
            var select = new JsonSelect(selectors);
            
            using (var jsonMapper = new ObjectMapper())
            using (var jsonSelector = new JsonSelector())
            {
                jsonMapper.Pretty = true;
                var peter  = jsonMapper.Write(TestQuery.Peter);
                var result = jsonSelector.Select(peter, select);
                
                // --- path[0]  group by using [=>]
                AreEqual("['Gaming','Biking','Travelling','Biking','Surfing']", result[0].ToString());
                // result contains two groups returned as index ranges:
                // Group 0: [0 - 2]
                // Group 1: [3 - 4]    note: 4 = result[0].values.Count - 1
                AreEqual(new [] {0, 3},     result[0].groupIndices); // the start indices of groups
                AreEqual(5,                 result[0].values.Count);
                
                // --- path[1]  dont group by using [*]
                AreEqual("['Gaming','Biking','Travelling','Biking','Surfing']", result[1].ToString());
                // result contains no groups
                AreEqual(0,                 result[1].groupIndices.Count);
                
                // values of both results are equal
                AreEqual(result[0].values,  result[1].values);
            }
        }
    }
}