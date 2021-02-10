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
            using (TypeStore typeStore = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
            using (JsonReader reader = new JsonReader(typeStore))
            using (JsonWriter writer = new JsonWriter(typeStore)) {
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
                } {
                    IList<int> collection = new List<int>(new[] {1, 2, 3});
                    writer.Write(collection);
                    var result = reader.Read<IList<int>>(writer.Output);
                    AreEqual(collection, result);
                }
                /* {
                    var arraySegment = new ArraySegment<int>(new[] {1, 2, 3});
                    writer.Write(arraySegment);
                    var result = reader.Read<ArraySegment<int>>(writer.Output);
                    AreEqual(arraySegment, result);
                } */
            }
        }

        [Test]
        public void TestICollection() {
            using (TypeStore typeStore = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
            using (JsonReader reader = new JsonReader(typeStore))
            using (JsonWriter writer = new JsonWriter(typeStore)) {
                // --- ICollection<>
                {
                    ICollection<int> collection = new List<int>(new[] {1, 2, 3});
                    writer.Write(collection);
                    var result = reader.Read<ICollection<int>>(writer.Output);
                    AreEqual(collection, result);
                } {
                    var linkedList = new LinkedList<int>(new[] {1, 2, 3});
                    writer.Write(linkedList);
                    var result = reader.Read<LinkedList<int>>(writer.Output);
                    AreEqual(linkedList, result);
                } {
                    var linkedList = new HashSet<int>(new[] {1, 2, 3});
                    writer.Write(linkedList);
                    var result = reader.Read<HashSet<int>>(writer.Output);
                    AreEqual(linkedList, result);
                } {
                    var sortedSet = new SortedSet<int>(new[] {1, 2, 3});
                    writer.Write(sortedSet);
                    var result = reader.Read<SortedSet<int>>(writer.Output);
                    AreEqual(sortedSet, result);
                }
            }
        }
        
        [Test]
        public void TestIEnumerable() {
            using (TypeStore typeStore = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
            using (JsonWriter writer = new JsonWriter(typeStore)) {
                // --- IEnumerable<>
                {
                    IEnumerable<int> collection = new List<int>(new[] {1, 2, 3});
                    writer.Write(collection);
                    AreEqual("[1,2,3]", writer.Output.ToString());
                }
            }
        }

        [Test]
        public void TestStack() {
            using (TypeStore    typeStore   = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
            using (JsonReader   reader      = new JsonReader(typeStore))
            using (JsonWriter writer = new JsonWriter(typeStore)) {
                var stack = new Stack<int>(new[] {1, 2, 3});
                writer.Write(stack);
                var result = reader.Read<Stack<int>>(writer.Output);
                AreEqual(stack, result);
            }
        }
        
        [Test]
        public void TestQueue() {
            using (TypeStore    typeStore   = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
            using (JsonReader   reader      = new JsonReader(typeStore))
            using (JsonWriter writer = new JsonWriter(typeStore)) {
                var stack = new Queue<int>(new[] {1, 2, 3});
                writer.Write(stack);
                var result = reader.Read<Queue<int>>(writer.Output);
                AreEqual(stack, result);
            }
        }

        [Test]
        public void TestIDictionary() {
            using (TypeStore    typeStore   = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
            using (JsonReader   reader      = new JsonReader(typeStore))
            using (JsonWriter writer = new JsonWriter(typeStore)) {
                // --- IDictionary<>
                {
                    var dictionary = new Dictionary<string, int> { { "A", 1 }, { "B", 2 }, { "C", 3} };
                    writer.Write(dictionary);
                    var result = reader.Read<Dictionary<string, int>>(writer.Output);
                    AreEqual(dictionary, result);
                } {
                    IDictionary<string, int> dictionary = new Dictionary<string, int> { { "A", 1 }, { "B", 2 }, { "C", 3} };
                    writer.Write(dictionary);
                    var result = reader.Read<IDictionary<string, int>>(writer.Output);
                    AreEqual(dictionary, result);
                } {
                    var sortedDictionary = new SortedDictionary<string, int> { { "A", 1 }, { "B", 2 }, { "C", 3} };
                    writer.Write(sortedDictionary);
                    var result = reader.Read<Dictionary<string, int>>(writer.Output);
                    AreEqual(sortedDictionary, result);
                } {
                    var sortedList = new SortedList<string, int> { { "A", 1 }, { "B", 2 }, { "C", 3} };
                    writer.Write(sortedList);
                    var result = reader.Read<Dictionary<string, int>>(writer.Output);
                    AreEqual(sortedList, result);
                } /* {
                    var dictionary = new ConcurrentDictionary<string, int> { { "A", 1 }, { "B", 2 }, { "C", 3} };
                    writer.Write(dictionary);
                    var result = reader.Read<ConcurrentDictionary<string, int>>(writer.Output);
                    AreEqual(dictionary, result);
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