using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Diff;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestCollections : LeakTestsFixture
    {
        private void AssertNull<T>(JsonReader reader, JsonWriter writer) {
            using (var dst = new TestBytes()) {
                writer.Write(default(T), ref dst.bytes);
                var result = reader.Read<T>(dst.bytes);
                AreEqual(null, result);
            }
        }
        
        private void AssertCompare<T>(TypeStore typeStore, T left, T right) {
            using (var differ = new Differ(typeStore)) {
                var diff = differ.GetDiff(left, right);
                IsNull(diff);
            }
        }
        
        [Test]
        public void TestIList() {
            using (TypeStore    typeStore   = new TypeStore(new StoreConfig(TypeAccess.IL)))
            using (JsonReader   reader      = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            using (var          dst         = new TestBytes())
            {
                // --- IList<>
                {
                    var collection = new Collection<int>(new[] {1, 2, 3});
                    writer.Write(collection, ref dst.bytes);
                    var result = reader.Read<Collection<int>>(dst.bytes);
                    AreEqual(collection, result);
                    AssertCompare(typeStore, collection, result);
                    
                    AssertNull<Collection<int>>(reader, writer);
                } {
                    IList<int> collection = new List<int>(new[] {1, 2, 3});
                    writer.Write(collection, ref dst.bytes);
                    var result = reader.Read<IList<int>>(dst.bytes);
                    AreEqual(collection, result);
                    AssertCompare(typeStore, collection, result);
                    
                    AssertNull<IList<int>>(reader, writer);
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
            using (TypeStore    typeStore   = new TypeStore(new StoreConfig(TypeAccess.IL)))
            using (JsonReader   reader      = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            using (var          dst         = new TestBytes())
            {
                // --- ICollection<>
                {
                    ICollection<int> collection = new List<int>(new[] {1, 2, 3});
                    writer.Write(collection, ref dst.bytes);
                    var result = reader.Read<ICollection<int>>(dst.bytes);
                    AreEqual(collection, result);
                    AssertCompare(typeStore, collection, result);
                    
                    AssertNull<ICollection<int>>(reader, writer);
                } {
                    var linkedList = new LinkedList<int>(new[] {1, 2, 3});
                    writer.Write(linkedList, ref dst.bytes);
                    var result = reader.Read<LinkedList<int>>(dst.bytes);
                    AreEqual(linkedList, result);
                    AssertCompare(typeStore, linkedList, result);
                    
                    AssertNull<LinkedList<int>>(reader, writer);
                } {
                    var linkedList = new HashSet<int>(new[] {1, 2, 3});
                    writer.Write(linkedList, ref dst.bytes);
                    var result = reader.Read<HashSet<int>>(dst.bytes);
                    AreEqual(linkedList, result);
                    AssertCompare(typeStore, linkedList, result);
                    
                    AssertNull<HashSet<int>>(reader, writer);
                } {
                    var sortedSet = new SortedSet<int>(new[] {1, 2, 3});
                    writer.Write(sortedSet, ref dst.bytes);
                    var result = reader.Read<SortedSet<int>>(dst.bytes);
                    AreEqual(sortedSet, result);
                    AssertCompare(typeStore, sortedSet, result);
                    
                    AssertNull<SortedSet<int>>(reader, writer);
                }
            }
        }
        
        [Test]
        public void TestIEnumerable() {
            using (TypeStore    typeStore   = new TypeStore(new StoreConfig(TypeAccess.IL)))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            {
                // --- IEnumerable<>
                {
                    IEnumerable<int> collection = new List<int>(new[] {1, 2, 3});
                    var json1 = writer.Write(collection);
                    AreEqual("[1,2,3]", json1);
                    
                    var json2 = writer.Write<IEnumerable<int>>(null);
                    AreEqual("null", json2);
                }
            }
        }
        
        [Test]
        public void TestReadOnlyCollection() {
            using (TypeStore    typeStore   = new TypeStore(new StoreConfig(TypeAccess.IL)))
            using (JsonReader   reader      = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            using (var          dst         = new TestBytes())
            {
                {
                    var queue = new ConcurrentQueue<int>(new[] {1, 2, 3});
                    writer.Write(queue, ref dst.bytes);
                    var result = reader.Read<ConcurrentQueue<int>>(dst.bytes);
                    AreEqual(queue, result);
                    AssertCompare(typeStore, queue, result);
                    
                    AssertNull<ConcurrentQueue<int>>(reader, writer);
                } {
                    var stack = new ConcurrentStack<int>(new[] {1, 2, 3});
                    writer.Write(stack, ref dst.bytes);
                    var result = reader.Read<ConcurrentStack<int>>(dst.bytes);
                    var reverse = stack.Reverse();
                    AreEqual(reverse, result);
                    AssertCompare(typeStore, reverse, result);
                    
                    AssertNull<ConcurrentStack<int>>(reader, writer);
                } {
                    var stack = new ConcurrentBag<int>(new[] {1, 2, 3});
                    writer.Write(stack, ref dst.bytes);
                    var result = reader.Read<ConcurrentBag<int>>(dst.bytes);
                    var reverse = stack.Reverse();
                    AreEqual(reverse, result);
                    AssertCompare(typeStore, reverse, result);
                    
                    AssertNull<ConcurrentBag<int>>(reader, writer);
                }
            } 
        }

        [Test]
        public void TestStack() {
            using (TypeStore    typeStore   = new TypeStore(new StoreConfig(TypeAccess.IL)))
            using (JsonReader   reader      = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            using (var          dst         = new TestBytes())
            {
                var stack = new Stack<int>(new[] {1, 2, 3});
                writer.Write(stack, ref dst.bytes);
                var result = reader.Read<Stack<int>>(dst.bytes);
                var reverse = stack.Reverse();
                AreEqual(reverse, result);
                AssertCompare(typeStore, reverse, result);
                
                AssertNull<Stack<int>>(reader, writer);
            }
        }
        
        [Test]
        public void TestQueue() {
            using (TypeStore    typeStore   = new TypeStore(new StoreConfig(TypeAccess.IL)))
            using (JsonReader   reader      = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            using (var          dst         = new TestBytes())
            {
                var stack = new Queue<int>(new[] {1, 2, 3});
                writer.Write(stack, ref dst.bytes);
                var result = reader.Read<Queue<int>>(dst.bytes);
                AreEqual(stack, result);
                AssertCompare(typeStore, stack, result);
                
                AssertNull<Queue<int>>(reader, writer);
            }
        }

        [Test]
        public void TestIDictionary() {
            using (TypeStore    typeStore   = new TypeStore(new StoreConfig(TypeAccess.IL)))
            using (JsonReader   reader      = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            using (var          dst         = new TestBytes())
            {
                // --- IDictionary<>
                {
                    var dictionary = new Dictionary<string, int> { { "A", 1 }, { "B", 2 }, { "C", 3} };
                    writer.Write(dictionary, ref dst.bytes);
                    var result = reader.Read<Dictionary<string, int>>(dst.bytes);
                    AreEqual(dictionary, result);
                    
                    AssertNull<Dictionary<string, int>>(reader, writer);
                } {
                    IDictionary<string, int> dictionary = new Dictionary<string, int> { { "A", 1 }, { "B", 2 }, { "C", 3} };
                    writer.Write(dictionary, ref dst.bytes);
                    var result = reader.Read<IDictionary<string, int>>(dst.bytes);
                    AreEqual(dictionary, result);
                    
                    AssertNull<IDictionary<string, int>>(reader, writer);
                } {
                    var sortedDictionary = new SortedDictionary<string, int> { { "A", 1 }, { "B", 2 }, { "C", 3} };
                    writer.Write(sortedDictionary, ref dst.bytes);
                    var result = reader.Read<SortedDictionary<string, int>>(dst.bytes);
                    AreEqual(sortedDictionary, result);
                    
                    AssertNull<SortedDictionary<string, int>>(reader, writer);
                } {
                    var sortedList = new SortedList<string, int> { { "A", 1 }, { "B", 2 }, { "C", 3} };
                    writer.Write(sortedList, ref dst.bytes);
                    var result = reader.Read<SortedList<string, int>>(dst.bytes);
                    AreEqual(sortedList, result);
                    
                    AssertNull<SortedList<string, int>>(reader, writer);
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