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
                    ".child"
                });
                AreEqual(@"{""val2"":68}",  result[0]);
                AreEqual("69",              result[1]);
                AreEqual("94.0",            result[2]);
                AreEqual("true",            result[3]);
                AreEqual(@"""one""",        result[4]);
                AreEqual("null",            result[5]);
                
            }
        }
        
        public class Book
        {
            public string   title;
        }

        public class Store
        {
            public List<Book>   books;
            
            public void InitSample() {
                books = new List<Book>(new[] {
                    new Book {title = "The Lord of the Rings"},
                    new Book {title = "Moby Dick"}
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

                var result = jsonPath.Select(json, new [] {
                    ".books[*].title"
                });
            }
        }
    }
}