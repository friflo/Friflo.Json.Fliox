// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Transform.Tree;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable NotAccessedField.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public class TestJsonMerger
    {
        private  class MergePrimitives {
            public  int         intEqual;
            public  int         intNotEqual;
            public  bool        boolEqual;
            public  bool        boolNotEqual;
            public  string      strEqual;
            public  string      strEqualNull;
            public  string      strNotEqual;
            public  string      strOnlyLeft;
            public  string      strOnlyRight;
        }
        
        [Test]
        public void TestJsonMergePrimitives() {
            using (var typeStore        = new TypeStore()) 
            using (var differ           = new ObjectDiffer(typeStore))
            using (var mergeWriter      = new JsonMergeWriter(typeStore))
            using (var writer           = new ObjectWriter(typeStore))
            using (var merger           = new JsonMerger())
            {
                var left    = new MergePrimitives {
                    intEqual        = 1,
                    intNotEqual     = 2,
                    boolEqual       = true,
                    boolNotEqual    = true,
                    strEqual        = "Test",
                    strEqualNull    = null, 
                    strNotEqual     = "Str-1",
                    strOnlyLeft     = "only-left",
                    strOnlyRight    = null
                };
                var right   = new MergePrimitives {
                    intEqual        = 1,
                    intNotEqual     = 22,
                    boolEqual       = true,
                    boolNotEqual    = false,
                    strEqual        = "Test",
                    strEqualNull    = null,
                    strNotEqual     = "Str-2",
                    strOnlyLeft     = null,
                    strOnlyRight    = "only-right"
                };
                var expect =
@"{
    'intEqual':     1,
    'intNotEqual':  22,
    'boolEqual':    true,
    'boolNotEqual': false,
    'strEqual':     'Test',
    'strNotEqual':  'Str-2',
    'strOnlyRight': 'only-right'
}";
                expect = NormalizeJson(expect);

                PrepareMerge(left, right, differ, mergeWriter, writer, false, out var value, out var patch);
                var merge       = merger.Merge(value, patch);
                AreEqual(expect, merge.AsString());
                
                PrepareMerge(left, right, differ, mergeWriter, writer, true, out value, out patch);
                merge          = merger.Merge(value, patch);
                AreEqual(expect, merge.AsString());
                
                AssertAlloc(value, patch, 1, merger);
            }
        }
        
        private class MergeChild {
            public  int     val;
        }
        
        private  class MergeClasses {
            public  MergeChild  childEqual;
            public  MergeChild  childNotEqual;
            public  MergeChild  childEqualNull;
            public  MergeChild  childOnlyLeft;
            public  MergeChild  childOnlyRight;
        }
        
        [Test]
        public void TestJsonMergeClasses() {
            using (var typeStore        = new TypeStore()) 
            using (var differ           = new ObjectDiffer(typeStore))
            using (var mergeWriter      = new JsonMergeWriter(typeStore))
            using (var writer           = new ObjectWriter(typeStore))
            using (var merger           = new JsonMerger())
            {
                var left    = new MergeClasses {
                    childEqual      = new MergeChild { val = 1 },
                    childNotEqual   = new MergeChild { val = 2 },
                    childEqualNull  = null,
                    childOnlyLeft   = new MergeChild { val = 4 },
                    childOnlyRight  = null
                };
                var right   = new MergeClasses {
                    childEqual      = new MergeChild { val = 1 },
                    childNotEqual   = new MergeChild { val = 3 },
                    childEqualNull  = null,
                    childOnlyLeft   = null,
                    childOnlyRight  = new MergeChild { val = 5 },
                };
                var expect =
@"{
    'childEqual':       {'val': 1},
    'childNotEqual':    {'val': 3},
    'childOnlyRight':   {'val': 5}
}";
                expect = NormalizeJson(expect);

                PrepareMerge(left, right, differ, mergeWriter, writer, false, out var value, out var patch);
                var merge       = merger.Merge(value, patch);
                AreEqual(expect, merge.AsString());
                
                PrepareMerge(left, right, differ, mergeWriter, writer, true, out value, out patch);
                merge          = merger.Merge(value, patch);
                AreEqual(expect, merge.AsString());
                
                AssertAlloc(value, patch, 1, merger);
            }
        }
        
        private  class MergeArray {
            public  int[]           int1;
            public  int[]           int2;
            public  bool[]          bool1;
            public  bool[]          bool2;
            public  string[]        str1;
            public  string[]        str2;
            public  MergeChild[]    child1;
            public  MergeChild[]    child2;
        }
        
        [Test]
        public void TestJsonMergeArrays() {
            using (var typeStore        = new TypeStore()) 
            using (var differ           = new ObjectDiffer(typeStore))
            using (var mergeWriter      = new JsonMergeWriter(typeStore))
            using (var writer           = new ObjectWriter(typeStore))
            using (var merger           = new JsonMerger())
            {
                var left = new MergeArray {
                    int1    = new [] { 10 },
                    int2    = new [] { 20 },
                    bool1   = new [] { true },
                    bool2   = new [] { true },
                    str1    = new [] { "A" },
                    str2    = new [] { "B" },
                    child1  = new [] { new MergeChild { val = 100 }},
                    child2  = new [] { new MergeChild { val = 200 }}
                };
                var right = new MergeArray {
                    int1    = new [] { 10 },
                    int2    = new [] { 21 },
                    bool1   = new [] { true },
                    bool2   = new [] { false },
                    str1    = new [] { "A" },
                    str2    = new [] { "C" },
                    child1  = new [] { new MergeChild { val = 100 }},
                    child2  = new [] { new MergeChild { val = 201 }}
                };
                var expect =
@"{
    'int1':     [10],
    'int2':     [21],
    'bool1':    [true],
    'bool2':    [false],
    'str1':     ['A'],
    'str2':     ['C'],
    'child1':   [{'val':100}],
    'child2':   [{'val':201}]
}";
                expect = NormalizeJson(expect);
                
                PrepareMerge(left, right, differ, mergeWriter, writer, false, out var value, out var patch);
                var merge       = merger.Merge(value, patch);
                AreEqual(expect, merge.AsString());
                
                PrepareMerge(left, right, differ, mergeWriter, writer, true, out value, out patch);
                merge          = merger.Merge(value, patch);
                AreEqual(expect, merge.AsString());
                
                AssertAlloc(value, patch, 1, merger);
            }
        }
        
        [Test]
        public void TestJsonMergeValues() {
            using (var merger           = new JsonMerger())
            {
                {
                    var value   = new JsonValue("true");
                    var patch   = new JsonValue("false");
                    var merge   = merger.Merge(value, patch);
                    AreEqual("false", merge.AsString());
                }
                {
                    var value   = new JsonValue("1");
                    var patch   = new JsonValue("2");
                    var merge   = merger.Merge(value, patch);
                    AreEqual("2", merge.AsString());
                }
                {
                    var value   = new JsonValue("\"abc\"");
                    var patch   = new JsonValue("\"xyz\"");
                    var merge   = merger.Merge(value, patch);
                    AreEqual("\"xyz\"", merge.AsString());
                }
                {
                    var value   = new JsonValue("[]");
                    var patch   = new JsonValue("[47]");
                    var merge   = merger.Merge(value, patch);
                    AreEqual("[47]", merge.AsString());
                }
                {
                    var value   = new JsonValue("null");
                    var patch   = new JsonValue("true");
                    var merge   = merger.Merge(value, patch);
                    AreEqual("true", merge.AsString());
                }
                {
                    var value   = new JsonValue("{\"holy\":[[]]}");
                    var patch   = new JsonValue("{}");
                    var merge   = merger.Merge(value, patch);
                    AreEqual("{\"holy\":[[]]}", merge.AsString());
                }
            }
        }
        
        [Test]
        public void TestJsonMergeErrors() {
            using (var merger           = new JsonMerger())
            {
                // --- patch value errors
                {
                    var value   = new JsonValue("true");
                    var patch   = new JsonValue("x");
                    merger.Merge(value, patch);
                    AreEqual("patch value error: unexpected character while reading value. Found: x path: '(root)' at position: 1", merger.Error);
                }
                // --- merge value errors
                {
                    var value   = new JsonValue("y");
                    var patch   = new JsonValue("false");
                    merger.Merge(value, patch);
                    AreEqual("merge value error: unexpected character while reading value. Found: y path: '(root)' at position: 1", merger.Error);
                }
                {
                    var value   = new JsonValue("{\"key\":xxx}");
                    var patch   = new JsonValue("false");
                    merger.Merge(value, patch);
                    AreEqual("merge value error: unexpected character while reading value. Found: x path: 'key' at position: 8", merger.Error);
                }
                {
                    var value   = new JsonValue("{\"key\":[a]}");
                    var patch   = new JsonValue("false");
                    merger.Merge(value, patch);
                    AreEqual("merge value error: unexpected character while reading value. Found: a path: 'key[0]' at position: 9", merger.Error);
                }
            }
        }
        
        private static void PrepareMerge<T>(
                T               left,
                T               right,
                ObjectDiffer    differ,
                JsonMergeWriter mergeWriter,
                ObjectWriter    writer,
                bool            writeNullMembers,
            out JsonValue       value,  // left as JSON
            out JsonValue       patch)  // the merge patch - when merging to left the result is right
        {
            var diff    = differ.GetDiff(left, right, DiffKind.DiffArrays);
            patch       = mergeWriter.WriteMergePatch(diff);

            writer.Pretty           = true;
            writer.WriteNullMembers = writeNullMembers;
            value        = writer.WriteAsValue(left);
        }
        
        private static void AssertAlloc(JsonValue value, JsonValue patch, int count, JsonMerger merger) {
            merger.MergeBytes(value, patch);

            var start   = Mem.GetAllocatedBytes();
            for (int n = 0; n < count; n++) {
                merger.MergeBytes(value, patch);
            }
            var diff = Mem.GetAllocationDiff(start);
            Mem.NoAlloc(diff);
        }
        
        private static string NormalizeJson(string value) {
            return Regex.Replace(value, @"\s+", "").Replace('\'', '"');
        }
    }
}