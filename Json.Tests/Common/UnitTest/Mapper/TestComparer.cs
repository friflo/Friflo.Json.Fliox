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
                IsTrue(comparer.AreEqual(sample, sample));

                var sample2 = new SampleIL();
                sample2.Init();
                comparer.AreEqual(sample2, sample);
                AreEqual(35, comparer.diffs.Count);
            }
        }
    }
}