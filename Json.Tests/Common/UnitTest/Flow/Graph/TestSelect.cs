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
            using (var typeStore        = new TypeStore()) 
            using (var jsonWriter       = new ObjectWriter(typeStore))
            using (var scalarSelector   = new ScalarSelector())
            using (var jsonSelector     = new JsonSelector())
            {
                var sample = new SampleIL();
                var json = jsonWriter.Write(sample);
                var selectors = new[] {
                    ".childStructNull1",
                    ".childStructNull2.val2",
                    ".dbl",
                    ".bln",
                    ".enumIL1",
                    ".child",
                    ".unknown"
                };
                var scalarSelect = new ScalarSelect(selectors);
                var scalarResults = scalarSelector.Select(json, scalarSelect);
                
                // AreEqual(@"[{""val2"":68}]",    result[0].ToString());
                AreEqual(new object[] {69},     scalarResults[1].AsObjects());
                AreEqual(new object[] {94},     scalarResults[2].AsObjects());
                AreEqual(new object[] {true},   scalarResults[3].AsObjects());
                AreEqual(new object[] {"one"},  scalarResults[4].AsObjects());
                AreEqual(new object[] {null},   scalarResults[5].AsObjects());
                AreEqual(new object[] {},       scalarResults[6].AsObjects());
                
                var jsonSelect = new JsonSelect(selectors);
                var jsonResults = jsonSelector.Select(json, jsonSelect);
                
                AreEqual(new[] {@"{""val2"":68}"},      jsonResults[0].values);
                AreEqual(new[] {"69"},                  jsonResults[1].values);
                AreEqual(new[] {"94.0"},                jsonResults[2].values);
                AreEqual(new[] {"true"},                jsonResults[3].values);
                AreEqual(new[] {"one"},                 jsonResults[4].values);
                AreEqual(new[] {"null"},                jsonResults[5].values);
                AreEqual(new string[0],                 jsonResults[6].values);
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
            using (var jsonSelector = new ScalarSelector())
            {
                var store = new Store();
                store.InitSample();
                var json = jsonWriter.Write(store);
                var select = new ScalarSelect(new[] {
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
            AreEqual(new[]{"The Lord of the Rings", "Moby Dick"},                           result[0].AsObjects());
            AreEqual(new[]{"J. R. R. Tolkien", "Herman Melville"},                          result[1].AsObjects());
            AreEqual(new[]{"The Sermon","A Long-expected Party", "The Shadow of the Past"}, result[2].AsObjects());
            AreEqual(new object[] {},                                                       result[3].AsObjects());
        }

        [Test]
        public void TestArrayGroupSelect() {
            var selectors = new[] {
                ".children[=>].hobbies[*].name", // group by using [=>]
                ".children[*].hobbies[*].name"   // dont group by using [*]
            };
            var select = new ScalarSelect(selectors);
            
            using (var jsonMapper = new ObjectMapper())
            using (var jsonSelector = new ScalarSelector())
            {
                jsonMapper.Pretty = true;
                var peter  = jsonMapper.Write(TestQuery.Peter);
                var result = jsonSelector.Select(peter, select);
                
                // --- path[0]  group by using [=>]
                AreEqual(new [] {"Gaming", "Biking", "Travelling", "Biking", "Surfing"}, result[0].AsObjects());
                // result contains two groups returned as index ranges:
                // Group 0: [0 - 2]
                // Group 1: [3 - 4]    note: 4 = result[0].values.Count - 1
                AreEqual(new [] {0, 3},     result[0].groupIndices); // the start indices of groups
                AreEqual(5,                 result[0].values.Count);
                
                // --- path[1]  dont group by using [*]
                AreEqual(new [] {"Gaming", "Biking", "Travelling", "Biking", "Surfing"}, result[1].AsObjects());
                // result contains no groups
                AreEqual(0,                 result[1].groupIndices.Count);
                
                // values of both results are equal
                AreEqual(result[0].values,  result[1].values);
            }
        }
    }
}