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
            {
                var set = new HashSet<int>();
                set.Add(1);
                IEnumerator iter = set.GetEnumerator();
                iter.MoveNext();
            }
            {
                var queue = new Queue<int>();
                queue.Enqueue(1);
                IEnumerator iter = queue.GetEnumerator();
                iter.MoveNext();
            } {
                var stack = new Stack<int>();
                stack.Push(1);
                IEnumerator iter = stack.GetEnumerator();
                iter.MoveNext();
            } {
                var linkedList = new LinkedList<int>();
                linkedList.AddLast(1);
                var last = linkedList.Last;
                if (last != null)
                    linkedList.AddAfter(last, 2);
                IEnumerator iter = linkedList.GetEnumerator();
                iter.MoveNext();
            } {
                var sortedList = new SortedList<string, int>();
                sortedList.Add("a", 1);
                IEnumerator iter = sortedList.GetEnumerator();
                iter.MoveNext();
            } {
                var arrayList = new ArrayList();
                arrayList.Add(1);
                IEnumerator iter = arrayList.GetEnumerator();
                iter.MoveNext();
            }
            //
            {
                var sortedList = new SortedList();
                sortedList.Add("a", 1);
                IEnumerator iter = sortedList.GetEnumerator();
                iter.MoveNext();

            } 
        }
    }
}