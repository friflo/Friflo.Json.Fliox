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
            }
        }
    }
}