using System;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph.Internal;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class TestUtils
    {
#if !UNITY_2020_1_OR_NEWER
        [Test]
        public void TestDictionaryValueIterator() {
            var store = new PocStore(new MemoryDatabase());
            var readArticles = store.articles.Read();
            var read= readArticles.ReadId("none");
            var task = readArticles.ReadRef(a => a.producer);
            SubRefs subRefs;
            subRefs.AddTask("someTask", task);

            // ensure iterator does not allocate something on heap by boxing
            var startBytes = GC.GetAllocatedBytesForCurrentThread();
            foreach (var subRef in subRefs) {
            }
            var endBytes = GC.GetAllocatedBytesForCurrentThread();
            AreEqual(startBytes, endBytes);
        }
#endif
    }
}
