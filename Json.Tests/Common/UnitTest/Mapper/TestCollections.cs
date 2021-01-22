using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestCollections
    {

        [Test]
        public void Run() {
            AreEqual(1,1 );
            IList<int> iList = null;
            ISet<int> iSet = null;
            //
            var set = new HashSet<int>();
            var queue = new Queue<int>();
            var stack = new Stack<int>();
            var linkedList = new LinkedList<int>();
            //
            var arrayList = new ArrayList();
            //
            var sortedListObj = new SortedList();
            var sortedList = new SortedList<string, int>();
            

        }
    }
}