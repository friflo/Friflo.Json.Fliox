using System.Collections.Generic;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestDiffer
    {
        class DiffChild
        {
            public int childVal;
        }

        class DiffBase
        {
            public DiffChild child;

        }
        
        [Test]
        public void TestClass() {
            using (var typeStore = new TypeStore()) {
                var typeCache = new TypeCache(typeStore);
                var differ = new Differ(typeCache);
                {
                    var left  = new DiffBase {child = new DiffChild {childVal = 1}};
                    var right = new DiffBase {child = new DiffChild {childVal = 2}};

                    var diff = differ.GetDiff(left, right);
                    AreEqual(1, diff.children.Count);
                    AreEqual(1, diff.children[0].children.Count);
                }

                IsNull(differ.GetDiff(1, 1));
                
                IsNotNull(differ.GetDiff(1, 2));

                IsNull(differ.GetDiff("A", "A"));
                
                IsNotNull(differ.GetDiff("A", "B"));

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
@"/enumIL1             three -> one
/enumIL2              -> two
/childStructNull1    (object)
/childStructNull2    (object)
/nulDouble           20 -> 70
/nulDoubleNull        -> 71
/nulFloat            21 -> 72
/nulFloatNull         -> 73
/nulLong             22 -> 74
/nulLongNull          -> 75
/nulInt              23 -> 76
/nulIntNull           -> 77
/nulShort            24 -> 78
/nulShortNull         -> 79
/nulByte             25 -> 80
/nulByteNull          -> 81
/nulBool             True -> False
/nulBoolNull          -> True
/childStruct1        (object)
/childStruct2        (object)
/child               (object)
/childNull           (object)
/structIL            (object)
/dbl                 22,5 -> 94
/flt                 33,5 -> 95
/int64               10 -> 96
/int32               11 -> 97
/int16               12 -> 98
/int8                13 -> 99
";
                    AreEqual(expect, childrenDiff);
                }
            }
        }
        
        [Test]
        public void TestContainer() {
            using (var typeStore = new TypeStore()) {
                var typeCache = new TypeCache(typeStore);
                var differ = new Differ(typeCache);
                var list1 =  new List<int> { 1,  2,  3 };
                var list2 =  new List<int> { 1, 12, 13 };
                var diff = differ.GetDiff(list1, list2);
                IsNotNull(diff);
                AreEqual(2, diff.children.Count);
                var childrenDiff = diff.GetChildrenDiff(10);
                var expect = @"/1
/2
";
                AreEqual(expect, childrenDiff);
            }
        }
    }
}