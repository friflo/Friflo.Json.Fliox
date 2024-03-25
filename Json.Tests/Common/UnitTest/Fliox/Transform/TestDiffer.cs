// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable ConvertToConstant.Local
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
        }
        
        [Test]
        public void TestDiffClass() {
            using (var typeStore        = new TypeStore()) 
            using (var mapper           = new ObjectMapper(typeStore))
            using (var differ           = new ObjectDiffer(typeStore))
            using (var mergeWriter      = new JsonMergeWriter(typeStore))
            {
                var left  = new DiffBase {
                    child1 = new DiffChild(),   // not equal
                    child2 = new DiffChild(),   // not equal
                    child3 = new DiffChild(),   // equal object
                    child4 = null,              // equal null
                    child5 = new DiffChild(),   // only left
                    child6 = null,              // only right
                };
                var right = new DiffBase {
                    child1 = new DiffChild { intVal1 = 1 },
                    child2 = new DiffChild { intVal2 = 2 },
                    child3 = new DiffChild(),
                    child4 = null,
                    child5 = null,
                    child6 = new DiffChild(),
                };
               
                var diff    = differ.GetDiff(left, right, DiffKind.DiffArrays);
                
                AreEqual(4, diff.Children.Count);
                var diffText    = diff.TextIndent(20);
                var expectedDiff = @"
/                   {DiffBase} != {DiffBase}
/child1             {DiffChild} != {DiffChild}
/child1/intVal1     0 != 1
/child2             {DiffChild} != {DiffChild}
/child2/intVal2     0 != 2
/child5             {DiffChild} != null
/child6             null != {DiffChild}
"; 
                AreEqual(expectedDiff, diffText);
                
                var patch    = mergeWriter.WriteMergePatch(diff);
                var expectedJson =
                "{'child1':{'intVal1':1},'child2':{'intVal2':2},'child5':null,'child6':{'intVal1':0,'intVal2':0}}".Replace('\'', '\"'); 
                AreEqual(expectedJson, patch.AsString());
                
                AssertMergePatch(left, right, patch, mapper);
                
                var start = Mem.GetAllocatedBytes();
                for (int n = 0; n < 10; n++) {
                    differ.GetDiff(left, right, DiffKind.DiffArrays);
                }
                var diffAlloc = Mem.GetAllocationDiff(start);
                Mem.NoAlloc(diffAlloc);
                Console.WriteLine($"Diff allocations: {diffAlloc}");
                
                AssertMergePatchAlloc(diff, mergeWriter);
            }
        }
        
        class DiffArray
        {
            public int[]        array1   { get; set; }
            public int[]        array2   { get; set; }
            public int[]        array3   { get; set; }
            public int[]        array4   { get; set; }
            public int[]        array5   { get; set; }
        }
        
        [Test]
        public void TestDiffArray() {
            using (var typeStore        = new TypeStore()) 
            using (var mapper           = new ObjectMapper(typeStore))
            using (var differ           = new ObjectDiffer(typeStore))
            using (var mergeWriter      = new JsonMergeWriter(typeStore))
            {
                var left  = new DiffArray {
                    array1 = new [] {1,   2},   // not equal
                    array2 = new [] {11, 12},   // equal
                    array3 = null,              // equal - both null
                    array4 = new [] {22},       // left only
                    array5 = null               // right only
                };
                var right = new DiffArray {
                    array1 = new [] {1,   3},
                    array2 = new [] {11, 12},
                    array3 = null,
                    array4 = null,
                    array5 = new [] {33}
                };
                
                var diff            = differ.GetDiff(left, right, DiffKind.DiffArrays);
                var diffText        = diff.TextIndent(20);
                var expectedDiff    = @"
/                   {DiffArray} != {DiffArray}
/array1             Int32[](count: 2) != Int32[](count: 2)
/array1/1           2 != 3
/array4             Int32[](count: 1) != null
/array5             null != Int32[](count: 1)
";
                AreEqual(expectedDiff, diffText);
                
                var patch   = mergeWriter.WriteMergePatch(diff);
                
                var expected= "{'array1':[1,3],'array4':null,'array5':[33]}".Replace('\'', '\"');
                AreEqual(expected, patch.ToString());
                AssertMergePatch(left, right, patch, mapper);
                AssertMergePatchAlloc(diff, mergeWriter);
            }
        }
        
        class DiffDictionary
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
            using (var mergeWriter      = new JsonMergeWriter(typeStore))
            {
                var left  = new DiffDictionary {
                    dict1 = new Dictionary<string, string>{{"key1", "A"}},  // not equal
                    dict2 = new Dictionary<string, string>{{"key2", "C"}},  // equal
                    dict3 = null,                                           // equal - both null
                    dict4 = new Dictionary<string, string>{{"key4", "D"}},  // only left
                    dict5 = null                                            // only right
                };
                var right = new DiffDictionary {
                    dict1 = new Dictionary<string, string>{{"key1", "B"}},
                    dict2 = new Dictionary<string, string>{{"key2", "C"}},
                    dict3 = null,
                    dict4 = null,
                    dict5 = new Dictionary<string, string>{{"key5", "E"}},
                };
                
                var diff            = differ.GetDiff(left, right, DiffKind.DiffArrays);
                var diffText        = diff.TextIndent(20);
                var expectedDiff    = @"
/                   {DiffDictionary} != {DiffDictionary}
/dict1              Dictionary<String, String> != Dictionary<String, String>
/dict1/key1         'A' != 'B'
/dict4              Dictionary<String, String> != null
/dict5              null != Dictionary<String, String>
";
                AreEqual(expectedDiff, diffText);
                
                var patch   = mergeWriter.WriteMergePatch(diff);
                var expected= "{'dict1':{'key1':'B'},'dict4':null,'dict5':{'key5':'E'}}".Replace('\'', '\"');
                AreEqual(expected, patch.ToString());
                AssertMergePatch(left, right, patch, mapper);
                AssertMergePatchAlloc(diff, mergeWriter);
            }
        }
        
        class DiffEntityInt
        {
            public int  id  { get; set; }
            public int  val { get; set; }
        }
        
        [Test]
        public void TestDiffEntityInt() {
            using (var typeStore        = new TypeStore()) 
            using (var mapper           = new ObjectMapper(typeStore))
            using (var differ           = new ObjectDiffer(typeStore))
            using (var mergeWriter      = new JsonMergeWriter(typeStore))
            {
                var left  = new DiffEntityInt { id = 1, val = 10, };
                var right = new DiffEntityInt { id = 1, val = 11, };
                var diff            = differ.GetDiff(left, right, DiffKind.DiffArrays);
                var diffText        = diff.TextIndent(20);
                var expectedDiff    = @"
/                   {DiffEntityInt} != {DiffEntityInt}
/val                10 != 11
";
                AreEqual(expectedDiff, diffText);
                
                var patch   = mergeWriter.WriteEntityMergePatch(diff, left);
                var expected= "{'id':1,'val':11}".Replace('\'', '\"');
                AreEqual(expected, patch.ToString());
                AssertMergePatch(left, right, patch, mapper);
                AssertMergePatchAlloc(diff, mergeWriter);
            }
        }
        
        class DiffEntityString
        {
            [Key]   public string   strId   { get; set; }
                    public int      val     { get; set; }
        }
        
        [Test]
        public void TestDiffEntityString() {
            using (var typeStore        = new TypeStore()) 
            using (var mapper           = new ObjectMapper(typeStore))
            using (var differ           = new ObjectDiffer(typeStore))
            using (var mergeWriter      = new JsonMergeWriter(typeStore))
            {
                var left  = new DiffEntityString { strId = "A", val = 10, };
                var right = new DiffEntityString { strId = "A", val = 11, };
                var diff            = differ.GetDiff(left, right, DiffKind.DiffArrays);
                var diffText        = diff.TextIndent(20);
                var expectedDiff    = @"
/                   {DiffEntityString} != {DiffEntityString}
/val                10 != 11
";
                AreEqual(expectedDiff, diffText);
                
                var patch   = mergeWriter.WriteEntityMergePatch(diff, left);
                var expected= "{'strId':'A','val':11}".Replace('\'', '\"');
                AreEqual(expected, patch.ToString());
                AssertMergePatch(left, right, patch, mapper);
                AssertMergePatchAlloc(diff, mergeWriter);
            }
        }
        
        private static void AssertMergePatch<T>(T left, T right, JsonValue patch, ObjectMapper mapper) {
            // create a copy of left to leave original instance unchanged
            var leftJson        = mapper.Write(left); 
            var leftCopy        = mapper.Read<T>(leftJson);
            // merge the JSON diff to the left copy
            var merge           = mapper.ReadTo(patch, leftCopy, false);
            // create JSON of merge and expected result to assert equality
            var mergeJson       = mapper.Write(merge);
            var expectedJson    = mapper.Write(right);
            
            AreEqual(expectedJson, mergeJson);
        }
        
        private static void AssertMergePatchAlloc(DiffNode diff, JsonMergeWriter mergeWriter) {
            var start = Mem.GetAllocatedBytes();
            mergeWriter.WriteMergePatchBytes(diff);
            var alloc = Mem.GetAllocationDiff(start);
            Mem.NoAlloc(alloc);
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
                var json    = mapper.WriteAsValue(value);
                
                var result  = new DiffBase();
                
                mapper.ReadTo(json, result, false);
                var start = Mem.GetAllocatedBytes();
                for (int n = 0; n < 10; n++) {
                    mapper.ReadTo(json, result, false);
                }
                var diffAlloc = Mem.GetAllocationDiff(start);
                // Mem.NoAlloc(diffAlloc);
                Console.WriteLine($"Diff allocations: {diffAlloc}");
            }
        }
    }
}