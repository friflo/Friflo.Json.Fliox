using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Diff;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestDiffer : LeakTestsFixture
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
            using (var typeStore    = new TypeStore()) 
            using (var jsonPatcher  = new JsonPatcher(typeStore))
            using (var differ       = new Differ(typeStore))
            {
                jsonPatcher.mapper.Pretty = true;
                {
                    var left  = new DiffBase {child = new DiffChild {
                        childVal = 1,
                        bigInt = BigInteger.Parse("111"),
                        dateTime = DateTime.Parse("2021-03-18T16:30:00.000Z")
                    }};
                    var right = new DiffBase {child = new DiffChild {
                        childVal = 2,
                        bigInt = BigInteger.Parse("222"),
                        dateTime = DateTime.Parse("2021-03-18T16:40:00.000Z")
                    }};

                    var diff = differ.GetDiff(left, right);
                    AreEqual(1, diff.children.Count);

                    var childrenDiff = diff.children[0].GetChildrenDiff(20);
                    var expect =
@"/child/childVal     1 -> 2
/child/bigInt       111 -> 222
/child/dateTime     2021-03-18T16:30:00.000Z -> 2021-03-18T16:40:00.000Z
"; 
                    AreEqual(expect, childrenDiff);
                    AssertPatch(jsonPatcher, left, right);
                }

                IsNull(differ.GetDiff(1, 1));
                {
                    var diff = differ.GetDiff(1, 2);
                    IsNotNull(diff);
                    AreEqual("1 -> 2", diff.ToString());
                }
                IsNull(differ.GetDiff("A", "A"));
                {
                    var diff = differ.GetDiff("A", "B");
                    IsNotNull(diff);
                    AreEqual("A -> B", diff.ToString());
                }
                {
                    var sample = new SampleIL();
                    IsNull(differ.GetDiff(sample, sample));

                    var sample2 = new SampleIL();
                    sample2.Init();
                    var diff = differ.GetDiff(sample2, sample);
                    IsNotNull(diff);
                    AreEqual(29, diff.children.Count);
                    var childrenDiff = diff.GetChildrenDiff(20);
                    var expect =
@"/enumIL1            three -> one
/enumIL2            null -> two
/childStructNull1   null -> (object)
/childStructNull2   (object) -> (object)
/nulDouble          20.0 -> 70.0
/nulDoubleNull      null -> 71.0
/nulFloat           21.0 -> 72.0
/nulFloatNull       null -> 73.0
/nulLong            22 -> 74
/nulLongNull        null -> 75
/nulInt             23 -> 76
/nulIntNull         null -> 77
/nulShort           24 -> 78
/nulShortNull       null -> 79
/nulByte            25 -> 80
/nulByteNull        null -> 81
/nulBool            true -> false
/nulBoolNull        null -> true
/childStruct1       (object) -> (object)
/childStruct2       (object) -> (object)
/child              (object) -> null
/childNull          null -> (object)
/structIL           (object) -> (object)
/dbl                22.5 -> 94.0
/flt                33.5 -> 95.0
/int64              10 -> 96
/int32              11 -> 97
/int16              12 -> 98
/int8               13 -> 99
";
                    AreEqual(expect, childrenDiff);
                    AssertPatch(jsonPatcher, sample2, sample);
                }
            }
        }
        
        [Test]
        public void TestContainerDiffCount() {
            using (var typeStore    = new TypeStore())
            using (var differ       = new Differ(typeStore)) {
                var left  = new List<int> { 1,  2,  3 };
                var right = new List<int> { 1,  2 };
                var diff = differ.GetDiff(left, right);
                IsNotNull(diff);
                AreEqual("Count: 3 -> 2", diff.ToString());
            }
        }
        
        [Test]
        public void TestPatchContainer() {
            using (var typeStore    = new TypeStore())
            using (var jsonPatcher  = new JsonPatcher(typeStore)) {
                // --- List<>
                {
                    var left  = new List<int> {1,  2,  3};
                    var right = new List<int> {1, 12, 13};
                    AssertPatchContainer(jsonPatcher, left, right);
                }
                // --- IList<>
                {
                    // var left  = new Collection<int>(new[] {1,  2,  3});
                    // -> System.NotSupportedException : Collection is read-only.
                }
                // --- ICollection<>
                {
                    var left  = new LinkedList<int>(new[] {1,  2,  3});
                    var right = new LinkedList<int>(new[] {1, 12, 13});
                    AssertPatchContainer(jsonPatcher, left, right);
                } {
                    var left  = new HashSet<int>(new[] {1,  2,  3});
                    var right = new HashSet<int>(new[] {1, 12, 13});
                    AssertPatchContainer(jsonPatcher, left, right);
                } {
                    var left  = new SortedSet<int>(new[] {1,  2,  3});
                    var right = new SortedSet<int>(new[] {1, 12, 13});
                    // AssertPatchContainer(jsonPatcher, left, right); todo
                }
 
            }
        }

        private static void AssertPatchContainer<T>(JsonPatcher jsonPatcher, T left, T right) {
            var diff = jsonPatcher.differ.GetDiff(left, right);
            IsNotNull(diff);
            AreEqual(2, diff.children.Count);
            var childrenDiff = diff.GetChildrenDiff(10);
            var expect =
                @"/1        2 -> 12
/2        3 -> 13
";
            AreEqual(expect, childrenDiff);
            AssertPatch(jsonPatcher, left, right);
        }

        private static void AssertPatch<T>(JsonPatcher jsonPatcher, T left, T right) {
            List<Patch> patches = jsonPatcher.GetPatches(left, right);

            var jsonPatches = jsonPatcher.mapper.Write(patches);
            var destPatches = jsonPatcher.mapper.Read<List<Patch>>(jsonPatches);
            AssertUtils.Equivalent(patches, destPatches);
                    
            jsonPatcher.ApplyPatches(left, destPatches);
            AssertUtils.Equivalent(left, right);
        }
    }
}