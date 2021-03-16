using System.Collections.Generic;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestComparer
    {
        [Test]
        public void TestClass() {
            using (var typeStore = new TypeStore()) {
                var typeCache = new TypeCache(typeStore);
                var comparer = new Friflo.Json.Mapper.Map.Comparer(typeCache);
                var sample = new SampleIL();
                IsNull(comparer.GetDiff(sample, sample));

                var sample2 = new SampleIL();
                sample2.Init();
                var diff = comparer.GetDiff(sample2, sample);
                IsNotNull(diff);
                AreEqual(25, diff.items.Count);
            }
        }
        
        [Test]
        public void TestContainer() {
            using (var typeStore = new TypeStore()) {
                var typeCache = new TypeCache(typeStore);
                var comparer = new Friflo.Json.Mapper.Map.Comparer(typeCache);
                var list1 =  new List<int> { 1,  2,  3 };
                var list2 =  new List<int> { 1, 12, 13 };
                var diff = comparer.GetDiff(list1, list2);
                IsNotNull(diff);
                AreEqual(2, diff.items.Count);
            }
        }
    }
}