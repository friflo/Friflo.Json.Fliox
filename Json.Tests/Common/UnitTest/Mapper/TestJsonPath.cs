using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Select;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestJsonPath : LeakTestsFixture
    {
        [Test]
        public void TestScalar() {
            var str = new Scalar("hello");
            AreEqual("hello", str.AsString());
            AreEqual(ScalarType.String, str.type);
            
            var dbl = new Scalar(1.5);
            AreEqual(1.5, dbl.AsDouble());

            var lng = new Scalar(2);
            AreEqual(2, lng.AsLong());
            
            var bln = new Scalar(true);
            AreEqual(true, bln.AsBool());

            var undef = new Scalar();
            AreEqual(ScalarType.Undefined, undef.type);

            var dbl2 = new Scalar(2.0);
            
            var lng2 = new Scalar(2);
            
            AreEqual(0, lng2.CompareTo(lng2));
            AreEqual(0, dbl2.CompareTo(dbl2));
            AreEqual(0, dbl2.CompareTo(lng2));
            AreEqual(0, lng2.CompareTo(dbl2));
            
            var @true  = new Scalar(true);
            var @false = new Scalar(false);
            AreEqual( 0, @true.CompareTo(@true));
            AreEqual( 1, @true.CompareTo(@false));
            AreEqual(-1, @false.CompareTo(@true));
        }

        [Test]
        public void TestObjectSelect() {
            using (var typeStore    = new TypeStore()) 
            using (var jsonWriter   = new JsonWriter(typeStore))
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
                AreEqual(@"[{""val2"":68}]",    result[0].ToString());
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
            using (var jsonWriter   = new JsonWriter(typeStore))
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
                
                var selector = new JsonSelectorQuery(selectList);
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
    }
}