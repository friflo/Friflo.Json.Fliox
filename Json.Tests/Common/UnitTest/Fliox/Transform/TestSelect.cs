// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public class TestSelect : LeakTestsFixture
    {
        [Test]
        public void TestObjectSelect() {
            using (var typeStore        = new TypeStore()) 
            using (var jsonWriter       = new ObjectWriter(typeStore))
            using (var scalarSelector   = new ScalarSelector())
            using (var jsonSelector     = new JsonSelector())
            using (var objectSelector   = new MemberAccessor(typeStore))
            {
                var sample  = new SampleIL();
                var json    = new Utf8Json(jsonWriter.WriteAsArray(sample));
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
                
                AreEqual(new object[] {"(object)"}, scalarResults[0].AsObjects());
                AreEqual(new object[] {69},         scalarResults[1].AsObjects());
                AreEqual(new object[] {94},         scalarResults[2].AsObjects());
                AreEqual(new object[] {true},       scalarResults[3].AsObjects());
                AreEqual(new object[] {"one"},      scalarResults[4].AsObjects());
                AreEqual(new object[] {null},       scalarResults[5].AsObjects());
                AreEqual(new object[] {},           scalarResults[6].AsObjects());
                
                var jsonSelect = new JsonSelect(selectors);
                var jsonResults = jsonSelector.Select(json, jsonSelect);
                
                AreEqual(new[] {@"{""val2"":68}"},  jsonResults[0].values);
                AreEqual(new[] {"69"},              jsonResults[1].values);
                AreEqual(new[] {"94.0"},            jsonResults[2].values);
                AreEqual(new[] {"true"},            jsonResults[3].values);
                AreEqual(new[] {"one"},             jsonResults[4].values);
                AreEqual(new[] {"null"},            jsonResults[5].values);
                AreEqual(new string[0],             jsonResults[6].values);

                var objectSelect = new MemberAccess(selectors);
                var objectResults = objectSelector.GetValues(sample, objectSelect);
                AreEqual(@"{""val2"":68}",          objectResults[0].Json.AsString());
                AreEqual("69",                      objectResults[1].Json.AsString());
                AreEqual("94.0",                    objectResults[2].Json.AsString());
                AreEqual("true",                    objectResults[3].Json.AsString());
                AreEqual(@"""one""",                objectResults[4].Json.AsString());
                AreEqual("null",                    objectResults[5].Json.AsString());
                IsFalse(                            objectResults[6].Found);

                var e = Throws<InvalidOperationException>(() => _ = objectResults[6].Json);
                AreEqual("member not found. path: .unknown", e.Message);
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
            using (var typeStore        = new TypeStore()) 
            using (var jsonWriter       = new ObjectWriter(typeStore))
            using (var scalarSelector   = new ScalarSelector())
            using (var jsonSelector     = new JsonSelector())
            {
                var store = new Store();
                store.InitSample();
                var json        = new Utf8Json(jsonWriter.WriteAsArray(store));
                var selectors   = new[] {
                    ".books[*].title",
                    ".books[*].author",
                    ".books[*].chapters[*].name",
                    ".books[*].unknown"
                };
                
                // --- Scalar select
                var scalarSelect = new ScalarSelect(selectors);
                IReadOnlyList<ScalarSelectResult> scalarResults = new List<ScalarSelectResult>();
                for (int n = 0; n < 2; n++) {
                    scalarResults = scalarSelector.Select(json, scalarSelect);
                }
                AssertScalarResults(scalarResults);
                
                for (int n = 0; n < 2; n++) {
                     scalarSelector.Select(json, scalarSelect);
                     scalarResults = scalarSelect.Results; // alternative access to results
                }
                AssertScalarResults(scalarResults);
                
                // --- JSON select
                var jsonSelect = new JsonSelect(selectors);
                var jsonResults = new List<JsonSelectResult>();
                for (int n = 0; n < 2; n++) {
                    jsonSelector.Select(json, jsonSelect);
                    jsonResults = jsonSelect.Results; // alternative access to results
                }
                AssertJsonResults(jsonResults);
            }
        }

        private void AssertScalarResults(IReadOnlyList<ScalarSelectResult> result) {
            AreEqual(new[]{"The Lord of the Rings", "Moby Dick"},                           result[0].AsObjects());
            AreEqual(new[]{"J. R. R. Tolkien", "Herman Melville"},                          result[1].AsObjects());
            AreEqual(new[]{"The Sermon","A Long-expected Party", "The Shadow of the Past"}, result[2].AsObjects());
            AreEqual(new object[] {},                                                       result[3].AsObjects());
        }
        
        private void AssertJsonResults(List<JsonSelectResult> result) {
            AreEqual(new[]{"The Lord of the Rings", "Moby Dick"},                           result[0].values);
            AreEqual(new[]{"J. R. R. Tolkien", "Herman Melville"},                          result[1].values);
            AreEqual(new[]{"The Sermon","A Long-expected Party", "The Shadow of the Past"}, result[2].values);
            AreEqual(new object[] {},                                                       result[3].values);
        }

        [Test]
        public void TestGroupSelect() {
            var selectors = new[] {
                ".children[=>].hobbies[*].name", // group by using [=>]
                ".children[*].hobbies[*].name"   // don't group by using [*]
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
        
        [Test]
        public void TestNoAlloc() {
            var selectors = new[] {
                ".age",
                ".children[*].age"
            };
            var select = new ScalarSelect(selectors);
            var memLog = new MemoryLogger(10, 10, MemoryLog.Enabled);
            using (var jsonMapper   = new ObjectMapper())
            using (var jsonSelector = new ScalarSelector())
            {
                jsonMapper.Pretty = true;
                var peter  = new Utf8Json(jsonMapper.WriteAsArray(TestQuery.Peter));

                IReadOnlyList<ScalarSelectResult> result = new List<ScalarSelectResult>();
                for (int n = 0; n < 100; n++) {
                    result = jsonSelector.Select(peter, select);
                    memLog.Snapshot();
                }
                AreEqual(new [] {40},     result[0].AsObjects());
                AreEqual(new [] {20, 20}, result[1].AsObjects());
                memLog.AssertNoAllocations();
            }
        }
    }
}