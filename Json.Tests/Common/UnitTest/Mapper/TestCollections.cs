using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Friflo.Json.Mapper;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestCollections
    {
        [Test]
        public void TestIList() {
            using (TypeStore    typeStore   = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
            using (JsonReader   reader      = new JsonReader(typeStore))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            {
                // --- IList<>
                /* {
                    var roCollection = new ReadOnlyCollection<int>(new[] {1, 2, 3});
                    writer.Write(roCollection);
                    var result = reader.Read<ReadOnlyCollection<int>>(writer.Output);
                    AreEqual(roCollection, result);
                } */
                {
                    var collection = new Collection<int>(new[] {1, 2, 3});
                    writer.Write(collection);
                    var result = reader.Read<Collection<int>>(writer.Output);
                    AreEqual(collection, result);
                }
                /* {
                    var arraySegment = new ArraySegment<int>(new[] {1, 2, 3});
                    writer.Write(arraySegment);
                    var result = reader.Read<ArraySegment<int>>(writer.Output);
                    AreEqual(arraySegment, result);
                } */
                
                // --- ICollection<>
                {
                    var linkedList = new LinkedList<int>(new[] {1, 2, 3});
                    writer.Write(linkedList);
                    var result = reader.Read<Collection<int>>(writer.Output);
                    AreEqual(linkedList, result);
                } {
                    var linkedList = new HashSet<int>(new[] {1, 2, 3});
                    writer.Write(linkedList);
                    var result = reader.Read<HashSet<int>>(writer.Output);
                    AreEqual(linkedList, result);
                }
                {
                    var sortedSet = new SortedSet<int>(new[] {1, 2, 3});
                    writer.Write(sortedSet);
                    var result = reader.Read<HashSet<int>>(writer.Output);
                    AreEqual(sortedSet, result);
                }
                // --- ICollection
                /* {
                    var stack = new Stack<int>(new[] {1, 2, 3});
                    writer.Write(stack);
                    var result = reader.Read<Stack<int>>(writer.Output);
                    AreEqual(stack, result);
                } */
            }
        }

        [Test]
        public void CheckInterfaces() {
            AreEqual(1,1 );
            // IList<int> iList = null;
            // ISet<int> iSet = null;
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