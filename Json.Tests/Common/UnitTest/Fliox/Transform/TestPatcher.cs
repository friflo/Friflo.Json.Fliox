// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Numerics;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.Mapper.DiffKind;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public class TestPatcher : LeakTestsFixture
    {
        class DiffChild
        {
            public int          childVal;
            public BigInteger   bigInt;
            public DateTime     dateTime;
        }

        class DiffBase
        {
            public DiffChild    child;
        }
        
        [Test]
        public void TestClass() {
            using (var jsonPatcher      = new JsonPatcher())
            using (var typeStore        = new TypeStore()) 
            using (var mapper           = new ObjectMapper(typeStore))
            using (var objectPatcher    = new ObjectPatcher(typeStore))
            using (var differ           = new ObjectDiffer(typeStore))
            {
                mapper.Pretty = true;
                {
                    var left  = new DiffBase {child = new DiffChild {
                        childVal = 1,
                        bigInt = BigInteger.Parse("111"),
                        dateTime = DateTime.Parse("2021-03-18T16:30:00.000Z").ToUniversalTime()
                    }};
                    var right = new DiffBase {child = new DiffChild {
                        childVal = 2,
                        bigInt = BigInteger.Parse("222"),
                        dateTime = DateTime.Parse("2021-03-18T16:40:00.000Z").ToUniversalTime()
                    }};

                    var diff = differ.GetDiff(left, right, DiffElements);
                    AreEqual(1, diff.Children.Count);

                    var diffText    = diff.Children[0].TextIndent(20);
                    var expect      = @"
/child              {DiffChild} != {DiffChild}
/child/childVal     1 != 2
/child/bigInt       111 != 222
/child/dateTime     2021-03-18T16:30:00Z != 2021-03-18T16:40:00Z
"; 
                    AreEqual(expect, diffText);
                    
                    var rightJson = mapper.Write(right);
                    var leftPatched = PatchJson(jsonPatcher, objectPatcher, left, right, mapper);
                    AreEqual(rightJson, leftPatched);
                    
                    PatchObject(objectPatcher, left, right, mapper);
                    AssertUtils.Equivalent(left, right);
                }

                IsNull(differ.GetDiff(1, 1, DiffElements));
                {
                    var diff = differ.GetDiff(1, 2, DiffElements);
                    IsNotNull(diff);
                    AreEqual("1 != 2", diff.ToString());
                    var e = Throws<JsonReaderException>(() => { objectPatcher.ApplyDiff(1, diff); });
                    StringAssert.Contains("ReadTo() can only used on an JSON object or array. Found: ValueNumber path: '(root)'", e.Message);
                }
                IsNull(differ.GetDiff("A", "A", DiffElements));
                {
                    var diff = differ.GetDiff("A", "B", DiffElements);
                    IsNotNull(diff);
                    AreEqual("'A' != 'B'", diff.ToString());
                    var e = Throws<JsonReaderException>(() => { objectPatcher.ApplyDiff("A", diff); });
                    StringAssert.Contains("ReadTo() can only used on an JSON object or array. Found: ValueString path: '(root)'", e.Message);
                }
                {
                    var left = new SampleIL();
                    left.Init();
                    IsNull(differ.GetDiff(left, left, DiffElements));

                    var right = new SampleIL();
                    IsNull(differ.GetDiff(right, right, DiffElements));
                    
                    var rightJson = mapper.Write(right);
                    var leftPatched = PatchJson(jsonPatcher, objectPatcher, left, right, mapper);
                    AreEqual(rightJson, leftPatched);


                    var diff = differ.GetDiff(left, right, DiffElements);
                    IsNotNull(diff);
                    AreEqual(29, diff.Children.Count);
                    var diffText    = diff.TextIndent(24);
                    var expect      = @"
/                       {SampleIL} != {SampleIL}
/enumIL1                three != one
/enumIL2                null != two
/childStructNull1       null != {ChildStructIL}
/childStructNull2       {ChildStructIL} != {ChildStructIL}
/childStructNull2/val2  19 != 69
/nulDouble              20.5 != 70.5
/nulDoubleNull          null != 71.5
/nulFloat               21.5 != 72.5
/nulFloatNull           null != 73.5
/nulLong                22 != 74
/nulLongNull            null != 75
/nulInt                 23 != 76
/nulIntNull             null != 77
/nulShort               24 != 78
/nulShortNull           null != 79
/nulByte                25 != 80
/nulByteNull            null != 81
/nulBool                true != false
/nulBoolNull            null != true
/childStruct1           {ChildStructIL} != {ChildStructIL}
/childStruct1/val2      111 != 90
/childStruct2           {ChildStructIL} != {ChildStructIL}
/childStruct2/val2      112 != 91
/child                  {ChildIL} != null
/childNull              null != {ChildIL}
/structIL               {StructIL} != {StructIL}
/structIL/structInt     200 != 0
/structIL/child1        {ChildStructIL} != null
/structIL/childClass2   {ChildIL} != null
/dbl                    22.5 != 94.5
/flt                    33.5 != 95.5
/int64                  10 != 96
/int32                  11 != 97
/int16                  12 != 98
/int8                   13 != 99
";
                    AreEqual(expect, diffText);
                    PatchObject(objectPatcher, left, right, mapper);
                    AssertUtils.Equivalent(left, right);
                }
            }
        }
        
        [Test]
        public void TestContainerArrayCount() {
            using (var typeStore    = new TypeStore())
            using (var differ       = new ObjectDiffer(typeStore)) {
                var left  = new [] { 1,  2,  3 };
                var right = new [] { 1,  2 };
                var diff = differ.GetDiff(left, right, DiffElements);
                IsNotNull(diff);
                AreEqual("Int32[](count: 3) != Int32[](count: 2)", diff.ToString());
            }
        }
        
        [Test]
        public void TestContainerDiffCount() {
            using (var typeStore    = new TypeStore())
            using (var differ       = new ObjectDiffer(typeStore)) {
                var left  = new List<int> { 1,  2,  3 };
                var right = new List<int> { 1,  2 };
                var diff = differ.GetDiff(left, right, DiffElements);
                IsNotNull(diff);
                AreEqual("List<Int32>(count: 3) != List<Int32>(count: 2)", diff.ToString());
            }
        }
        
        [Test]
        public void TestPatchContainer() {
            using (var jsonPatcher      = new JsonPatcher())
            using (var typeStore        = new TypeStore())
            using (var mapper           = new ObjectMapper(typeStore))
            using (var objectPatcher    = new ObjectPatcher(typeStore)) {
                mapper.Pretty = true;
                // --- []
                {
                    var left  = new[] {1,  2,  3};
                    var right = new[] {1, 12, 13};
                    
                    var rightJson = mapper.Write(right);
                    var leftPatched = PatchJson(jsonPatcher, objectPatcher, left, right, mapper);
                    AreEqual(rightJson, leftPatched);
                    
                    var diff        = PatchElements(objectPatcher, left, right, mapper);
                    var diffText    = diff.TextIndent(8);
                    var expect      = @"
/       Int32[](count: 3) != Int32[](count: 3)
/1      2 != 12
/2      3 != 13
";
                    AreEqual(expect, diffText);
                    AssertUtils.Equivalent(left, right);
                }
                // --- List<>
                {
                    var left  = new List<int> {1,  2,  3};
                    var right = new List<int> {1, 12, 13};
                    
                    var rightJson = mapper.Write(right);
                    var leftPatched = PatchJson(jsonPatcher, objectPatcher, left, right, mapper);
                    AreEqual(rightJson, leftPatched);
                    
                    var diff        = PatchElements(objectPatcher, left, right, mapper);
                    var diffText    = diff.TextIndent(8);
                    var expect      = @"
/       List<Int32>(count: 3) != List<Int32>(count: 3)
/1      2 != 12
/2      3 != 13
";
                    AreEqual(expect, diffText);
                    AssertUtils.Equivalent(left, right);
                }
                // --- IList<>
                {
                    var left  = new List<int> {1,  2,  3};
                    var right = new List<int> {1, 12, 13};
                    
                    var rightJson = mapper.Write(right);
                    var leftPatched = PatchJson(jsonPatcher, objectPatcher, left, right, mapper);
                    AreEqual(rightJson, leftPatched);
                    
                    var diff        = PatchElements<IList<int>>(objectPatcher, left, right, mapper);
                    var diffText    = diff.TextIndent(8);
                    var expect      = @"
/       List<Int32>(count: 3) != List<Int32>(count: 3)
/1      2 != 12
/2      3 != 13
";
                    AreEqual(expect, diffText);
                    AssertUtils.Equivalent(left, right);
                } {
                    // var left  = new Collection<int>(new[] {1,  2,  3});
                    // -> System.NotSupportedException : Collection is read-only.
                }
                // --- ICollection<>
                {
                    var left  = new LinkedList<int>(new[] {1,  2,  3});
                    var right = new LinkedList<int>(new[] {1, 12, 13});
                    
                    var rightJson = mapper.Write(right);
                    var leftPatched = PatchJson(jsonPatcher, objectPatcher, left, right, mapper);
                    AreEqual(rightJson, leftPatched);

                    var diff        = PatchElements(objectPatcher, left, right, mapper);
                    var diffText    = diff.TextIndent(8);
                    var expect      = @"
/       LinkedList<Int32>(count: 3) != LinkedList<Int32>(count: 3)
/1      2 != 12
/2      3 != 13
";
                    AreEqual(expect, diffText);
                    AssertUtils.Equivalent(left, right);
                } {
                    var left  = new HashSet<int>(new[] {1,  2,  3});
                    var right = new HashSet<int>(new[] {1, 12, 13});
                    // the whole JSON array is added as a single Patch  
                    
                    var rightJson = mapper.Write(right);
                    var leftPatched = PatchJson(jsonPatcher, objectPatcher, left, right, mapper);
                    AreEqual(rightJson, leftPatched);
                    
                    var diff = PatchCollection(objectPatcher, left, right, mapper);
                    AreEqual("HashSet<Int32>(count: 3) != HashSet<Int32>(count: 3)", diff.ToString());

                    AssertUtils.Equivalent(left, right);
                } {
                    var left  = new SortedSet<int>(new[] {1,  2,  3});
                    var right = new SortedSet<int>(new[] {1, 12, 13});
                    // the whole JSON array is added as a single Patch  
                    
                    var rightJson = mapper.Write(right);
                    var leftPatched = PatchJson(jsonPatcher, objectPatcher, left, right, mapper);
                    AreEqual(rightJson, leftPatched);
                    
                    var diff = PatchCollection(objectPatcher, left, right, mapper);
                    AreEqual("SortedSet<Int32>(count: 3) != SortedSet<Int32>(count: 3)", diff.ToString());
                    AssertUtils.Equivalent(left, right);
                }
                // --- Stack<>
                {
                    var left  = new Stack<int>(new[] { 3,  2, 1});
                    var right = new Stack<int>(new[] {13, 12, 1});
                    
                    var rightJson = mapper.Write(right);
                    var leftPatched = PatchJson(jsonPatcher, objectPatcher, left, right, mapper);
                    AreEqual(rightJson, leftPatched);

                    var diff        = PatchElements(objectPatcher, left, right, mapper);
                    var diffText    = diff.TextIndent(8);
                    var expect      = @"
/       Stack<Int32>(count: 3) != Stack<Int32>(count: 3)
/1      2 != 12
/2      3 != 13
";
                    AreEqual(expect, diffText);
                    AssertUtils.Equivalent(left, right);
                }
                // --- Queue<>
                {
                    var left  = new Queue<int>(new[] {1,  2,  3});
                    var right = new Queue<int>(new[] {1, 12, 13});
                    
                    var rightJson = mapper.Write(right);
                    var leftPatched = PatchJson(jsonPatcher, objectPatcher, left, right, mapper);
                    AreEqual(rightJson, leftPatched);

                    var diff        = PatchElements(objectPatcher, left, right, mapper);
                    var diffText    = diff.TextIndent(8);
                    var expect      = @"
/       Queue<Int32>(count: 3) != Queue<Int32>(count: 3)
/1      2 != 12
/2      3 != 13
";
                    AreEqual(expect, diffText);
                    AssertUtils.Equivalent(left, right);
                }
            }
        }

        [Test]
        public void TestPatchDictionary() {
            using (var jsonPatcher      = new JsonPatcher())
            using (var typeStore        = new TypeStore())
            using (var mapper           = new ObjectMapper(typeStore))
            using (var objectPatcher    = new ObjectPatcher(typeStore)) {
                mapper.Pretty = true;
                {
                    var left  = new Dictionary<string, int> {{"A", 1}, {"C",  3}};
                    var right = new Dictionary<string, int> {{"A", 2}, {"B", 12}};
                    
                    var rightJson = mapper.Write(right);
                    var leftPatched = PatchJson(jsonPatcher, objectPatcher, left, right, mapper);
                    AreEqual(rightJson, leftPatched);
                    
                    var diff        = PatchKeyValues(objectPatcher, left, right);
                    var diffText    = diff.TextIndent(8);
                    var expect      = @"
/       Dictionary<String, Int32> != Dictionary<String, Int32>
/A      1 != 2
/C      3 != (missing)
/B      (missing) != 12
";
                    AreEqual(expect, diffText);
                    AssertUtils.Equivalent(left, right);
                } {
                    var left  = new Dictionary<string, int> {{"A", 1}, {"C",  3}};
                    var right = new Dictionary<string, int> {{"A", 2}, {"B", 12}};
                    
                    var rightJson = mapper.Write(right);
                    var leftPatched = PatchJson(jsonPatcher, objectPatcher, left, right, mapper);
                    AreEqual(rightJson, leftPatched);
                    
                    var diff        = PatchKeyValues<IDictionary<string, int>>(objectPatcher, left, right);
                    var diffText    = diff.TextIndent(8);
                    var expect      = @"
/       Dictionary<String, Int32> != Dictionary<String, Int32>
/A      1 != 2
/C      3 != (missing)
/B      (missing) != 12
";
                    AreEqual(expect, diffText);
                    AssertUtils.Equivalent(left, right);
                } {
                    var left  = new SortedDictionary<string, int> {{"A", 1}, {"C",  3}};
                    var right = new SortedDictionary<string, int> {{"A", 2}, {"B", 12}};
                    
                    var rightJson = mapper.Write(right);
                    var leftPatched = PatchJson(jsonPatcher, objectPatcher, left, right, mapper);
                    AreEqual(rightJson, leftPatched);
                    
                    var diff        = PatchKeyValues(objectPatcher, left, right);
                    var diffText    = diff.TextIndent(8);
                    var expect      = @"
/       SortedDictionary<String, Int32> != SortedDictionary<String, Int32>
/A      1 != 2
/C      3 != (missing)
/B      (missing) != 12
";
                    AreEqual(expect, diffText);
                    AssertUtils.Equivalent(left, right);
                } {
                    var left  = new SortedList<string, int> {{"A", 1}, {"C",  3}};
                    var right = new SortedList<string, int> {{"A", 2}, {"B", 12}};
                    
                    var rightJson = mapper.Write(right);
                    var leftPatched = PatchJson(jsonPatcher, objectPatcher, left, right, mapper);
                    AreEqual(rightJson, leftPatched);
                    
                    var diff        = PatchKeyValues(objectPatcher, left, right);
                    var diffText    = diff.TextIndent(8);
                    var expect      = @"
/       SortedList<String, Int32> != SortedList<String, Int32>
/A      1 != 2
/C      3 != (missing)
/B      (missing) != 12
";
                    AreEqual(expect, diffText);
                    AssertUtils.Equivalent(left, right);
                }
            }
        }

        private static DiffNode PatchElements<T>(ObjectPatcher objectPatcher, T left, T right, ObjectMapper mapper) {
            var diff = objectPatcher.differ.GetDiff(left, right, DiffElements);
            IsNotNull(diff);
            AreEqual(DiffType.None, diff.DiffType); // DiffType.None -> replace container elements
            AreEqual(2, diff.Children.Count);
            PatchObject(objectPatcher, left, right, mapper);
            return diff;
        }
        
        private static DiffNode PatchCollection<T>(ObjectPatcher objectPatcher, T left, T right, ObjectMapper mapper) {
            var diff = objectPatcher.differ.GetDiff(left, right, DiffElements);
            IsNotNull(diff);
            AreEqual(DiffType.NotEqual, diff.DiffType); // DiffType.NotEqual -> replace whole collection
            AreEqual(1, diff.Children.Count);           // skipped comparing elements after first difference
            PatchObject(objectPatcher, left, right, mapper);
            return diff;
        }
        
        private static DiffNode PatchKeyValues<T>(ObjectPatcher objectPatcher, T left, T right) {
            var diff = objectPatcher.differ.GetDiff(left, right, DiffElements);
            IsNotNull(diff);
            AreEqual(3, diff.Children.Count);

            var patches = objectPatcher.CreatePatches(diff);
            objectPatcher.ApplyPatches(left, patches);
            return diff;
        }
        
        private static void PatchObject<T>(ObjectPatcher objectPatcher, T left, T right, ObjectMapper mapper)
        {
            List<JsonPatch> patches = objectPatcher.GetPatches(left, right);

            var jsonPatches = mapper.Write(patches);
            var destPatches = mapper.Read<List<JsonPatch>>(jsonPatches);
            AssertUtils.Equivalent(patches, destPatches);
                    
            objectPatcher.ApplyPatches(left, destPatches);
        }
        
        private static string PatchJson<T>(JsonPatcher jsonPatcher, ObjectPatcher objectPatcher, T left, T right, ObjectMapper mapper)
        {
            List<JsonPatch> patches = objectPatcher.GetPatches(left, right);
            var leftJson = mapper.WriteAsValue(left);
            
            var jsonPatches = mapper.Write(patches);
            var destPatches = mapper.Read<List<JsonPatch>>(jsonPatches);
            AssertUtils.Equivalent(patches, destPatches);
            
            var leftPatched = jsonPatcher.ApplyPatches(leftJson, patches, true);
            return leftPatched.AsString();
        }
    }
}