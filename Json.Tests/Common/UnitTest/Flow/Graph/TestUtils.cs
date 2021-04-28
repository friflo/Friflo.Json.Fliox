using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Graph.Internal;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class TestUtils
    {

        
        [Test]
        public void TestDictionaryValueIterator() {
            var store = new PocStore(new MemoryDatabase());
            var read= store.articles.Read("none");
            var task = read.ReadRef(a => a.producer);
            SubRefs subRefs;
            subRefs.AddTask("someTask", task);

            // ensure iterator does not allocate something on heap by boxing
            var startBytes = GC.GetAllocatedBytesForCurrentThread();
            foreach (var subRef in subRefs) {
            }
            var endBytes = GC.GetAllocatedBytesForCurrentThread();
            AreEqual(startBytes, endBytes);
        }
    }
}
