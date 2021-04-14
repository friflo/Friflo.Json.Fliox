using System.Collections.Generic;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Tests.Common.UnitTest.Mapper;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Graph
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

                var result = jsonSelector.Select(json, new [] {
                    ".childStructNull1",
                    ".childStructNull2.val2",
                    ".dbl",
                    ".bln",
                    ".enumIL1",
                    ".child",
                    ".unknown"
                });
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
                var selectList = new[] {
                    ".books[*].title",
                    ".books[*].author",
                    ".books[*].chapters[*].name",
                    ".books[*].unknown"
                };
                var result = new List<SelectorResult>();
                for (int n = 0; n < 2; n++) {
                    result = jsonSelector.Select(json, selectList);
                }
                AssertStoreResult(result);
                
                var selector = new JsonSelect(selectList);
                for (int n = 0; n < 2; n++) {
                    jsonSelector.Select(json, selector);
                    result = selector.GetResult();
                }
                AssertStoreResult(result);
            }
        }

        private void AssertStoreResult(List<SelectorResult> result) {
            AreEqual("['The Lord of the Rings','Moby Dick']",                           result[0].ToString());
            AreEqual("['J. R. R. Tolkien','Herman Melville']",                          result[1].ToString());
            AreEqual("['The Sermon','A Long-expected Party','The Shadow of the Past']", result[2].ToString());
            AreEqual("[]",                                                              result[3].ToString());
        }

        [Test]
        public void TestArrayGroupSelect() {
            var selectList = new[] {
                ".children[@].hobbies[*].name",
                ".children[@]",
            };
            var select = new JsonSelect(selectList);
            
            using (var jsonMapper = new ObjectMapper())
            using (var jsonSelector = new JsonSelector())
            {
                jsonMapper.Pretty = true;
                var peter = jsonMapper.Write(TestQuery.Peter);
                var john = jsonMapper.Write(TestQuery.John);
                jsonSelector.Select(peter, select);
                var result = select.GetResult();
                AreEqual("['Biking','Surfing','Biking']", result[0].ToString());
                AreEqual("[(object),(object)]", result[1].ToString());
            }
        }
    }
}