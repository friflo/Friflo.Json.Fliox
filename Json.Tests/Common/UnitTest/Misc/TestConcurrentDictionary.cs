using System;
using System.Collections.Concurrent;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Misc
{
    public class TestConcurrentDictionary
    {
        [Test]
        public void ConcurrentDictionaryAlloc()
        {
            string abc      = "abc";
            string efg      = "efg";
            string xyz      = "xyz";
            var set = new ConcurrentDictionary<string, string> {
                [abc] = abc,
                [efg] = efg,
                [xyz] = xyz,
            };
            set.GetOrAdd("key", "key");

            var start = GC.GetAllocatedBytesForCurrentThread();
            var keyAbc = "abc";
            var valAbc = set.GetOrAdd("abc", keyAbc);

            var dif  = GC.GetAllocatedBytesForCurrentThread() - start;
            
            AreSame(valAbc, abc);
            AreEqual(0, dif);
        }
    }
}