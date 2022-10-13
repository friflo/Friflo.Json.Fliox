// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public class TestDiffer : LeakTestsFixture
    {
        class DiffChild
        {
            public int          intVal1 { get; set; }
            public int          intVal2 { get; set; }
        }

        class DiffBase
        {
            public DiffChild    child1   { get; set; }
            public DiffChild    child2   { get; set; }
            public DiffChild    child3   { get; set; }
            public DiffChild    child4   { get; set; }
            public DiffChild    child5   { get; set; }
            public DiffChild    child6   { get; set; }
            
            public int[]        array1   { get; set; }
            public int[]        array2   { get; set; }
            public int[]        array3   { get; set; }
            public int[]        array4   { get; set; }
            public int[]        array5   { get; set; }
        }
        
        [Test]
        public void TestDiff() {
            using (var typeStore        = new TypeStore()) 
            using (var mapper           = new ObjectMapper(typeStore))
            using (var differ           = new ObjectDiffer(typeStore))
            using (var jsonDiff         = new JsonDiff(typeStore))
            {
                var left  = new DiffBase {
                    child1 = new DiffChild(),   // not equal
                    child2 = new DiffChild(),   // not equal
                    child3 = new DiffChild(),   // equal object
                    child4 = null,              // equal null
                    child5 = new DiffChild(),   // only left
                    child6 = null,              // only right
                    
                    array1 = new [] {1,   2},   // not equal
                    array2 = new [] {11, 12},   // equal
                    array3 = null,              // equal - both null
                    array4 = new [] {22},       // left only
                    array5 = null               // right only
                };
                var right = new DiffBase {
                    child1 = new DiffChild { intVal1 = 1 },
                    child2 = new DiffChild { intVal2 = 2 },
                    child3 = new DiffChild(),
                    child4 = null,
                    child5 = null,
                    child6 = new DiffChild(),
                    
                    array1 = new [] {1,   3},
                    array2 = new [] {11, 12},
                    array3 = null,
                    array4 = null,
                    array5 = new [] {33}
                };
               
                var diff    = differ.GetDiff(left, right, DiffKind.DiffArrays);
                
                AreEqual(7, diff.Children.Count);
                var diffText    = diff.TextIndent(20);
                var expectedDiff = @"
/                   {DiffBase} != {DiffBase}
/child1             {DiffChild} != {DiffChild}
/child1/intVal1     0 != 1
/child2             {DiffChild} != {DiffChild}
/child2/intVal2     0 != 2
/child5             {DiffChild} != null
/child6             null != {DiffChild}
/array1             Int32[](count: 2) != Int32[](count: 2)
/array1/1           2 != 3
/array4             Int32[](count: 1) != null
/array5             null != Int32[](count: 1)
"; 
                AreEqual(expectedDiff, diffText);
                
                var json    = jsonDiff.CreateJsonDiff(diff);
                var expectedJson =
                "{'child1':{'intVal1':1},'child2':{'intVal2':2},'child5':null,'child6':{'intVal1':0,'intVal2':0},'array1':[1,3],'array4':null,'array5':[33]}".Replace('\'', '\"'); 
                AreEqual(expectedJson, json.AsString());
                
                MergeDiff(left, right, differ, mapper, jsonDiff);
                
                var start = GC.GetAllocatedBytesForCurrentThread();
                for (int n = 0; n < 10; n++) {
                    differ.GetDiff(left, right, DiffKind.DiffArrays);
                }
                var diffAlloc =  GC.GetAllocatedBytesForCurrentThread() - start;
                AreEqual(0, diffAlloc);
                Console.WriteLine($"Diff allocations: {diffAlloc}");
            }
        }
        
        class DiffDict
        {
            public Dictionary<string, string>   dict1   { get; set; }
            public Dictionary<string, string>   dict2   { get; set; }
            public Dictionary<string, string>   dict3   { get; set; }
            public Dictionary<string, string>   dict4   { get; set; }
            public Dictionary<string, string>   dict5   { get; set; }
        }
        
        [Test]
        public void TestDiffDictionary() {
            using (var typeStore        = new TypeStore()) 
            using (var mapper           = new ObjectMapper(typeStore))
            using (var differ           = new ObjectDiffer(typeStore))
            using (var jsonDiff         = new JsonDiff(typeStore))
            {
                var left  = new DiffDict {
                    dict1 = new Dictionary<string, string>{{"key1", "A"}},  // not equal
                    dict2 = new Dictionary<string, string>{{"key2", "C"}},  // equal
                    dict3 = null,                                           // equal - both null
                    dict4 = new Dictionary<string, string>{{"key4", "D"}},  // only left
                    dict5 = null                                            // only right
                };
                var right = new DiffDict {
                    dict1 = new Dictionary<string, string>{{"key1", "B"}},
                    dict2 = new Dictionary<string, string>{{"key2", "C"}},
                    dict3 = null,
                    dict4 = null,
                    dict5 = new Dictionary<string, string>{{"key5", "E"}},
                };
                
                var diff            = MergeDiff(left, right, differ, mapper, jsonDiff);
                var diffText        = diff.TextIndent(20);
                var expectedDiff    = @"
/                   {DiffDict} != {DiffDict}
/dict1              Dictionary<String, String> != Dictionary<String, String>
/dict1/key1         'A' != 'B'
/dict4              Dictionary<String, String> != null
/dict5              null != Dictionary<String, String>
";
                AreEqual(expectedDiff, diffText);
                
                var patch   = jsonDiff.CreateJsonDiff(diff);
                var expected= "{'dict1':{'key1':'B'},'dict4':null,'dict5':{'key5':'E'}}".Replace('\'', '\"');
                AreEqual(expected, patch.ToString());
            }
        }
        
        private static DiffNode MergeDiff<T>(T left, T right, ObjectDiffer differ, ObjectMapper mapper, JsonDiff jsonDiff) {
            // create JSON diff from DiffNode
            var diff            = differ.GetDiff(left, right, DiffKind.DiffArrays);
            var patch           = jsonDiff.CreateJsonDiff(diff);
            // create a copy of left to leave original instance unchanged
            var leftJson        = mapper.Write(left);       // for testing only 
            var leftCopy        = mapper.Read<T>(leftJson); // for testing only
            // merge the JSON diff to the left copy
            var merge           = mapper.ReadTo(patch, leftCopy);
            // create JSON of merge and expected result to assert equality
            var mergeJson       = mapper.Write(merge);      // for testing only
            var expectedJson    = mapper.Write(right);      // for testing only
            
            AreEqual(expectedJson, mergeJson);
            return diff;
        }
        
        [Test]
        public void TestMapperPerf() {
            using (var typeStore        = new TypeStore(new StoreConfig())) 
            using (var mapper           = new ObjectMapper(typeStore)) {
                var value  = new DiffBase {
                    child1 = new DiffChild {
                        intVal1 = 111,
                        intVal2 = 222,
                    },
                    child2 = new DiffChild {
                        intVal1 = 333,
                        intVal2 = 444,
                    }
                };
                var array = mapper.WriteAsArray(value);
                var json = new JsonValue(array);
                
                var result  = new DiffBase();
                
                mapper.ReadTo(json, result);
                var start = GC.GetAllocatedBytesForCurrentThread();
                for (int n = 0; n < 10; n++) {
                    mapper.ReadTo(json, result);
                }
                var diffAlloc =  GC.GetAllocatedBytesForCurrentThread() - start;
                // AreEqual(0, diffAlloc);
                Console.WriteLine($"Diff allocations: {diffAlloc}");
            }
        }
    }
}