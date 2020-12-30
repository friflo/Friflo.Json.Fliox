#if UNITY_5_3_OR_NEWER

using System;
using Friflo.Json.Tests.Common;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using Unity.Collections;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Unity
{
    public struct HybridArray<T> : IDisposable where T: struct 
    {
        private T[] array;

        public HybridArray(int size) {
            array = new T[size];
        }
	    
        public T this[int index]
        {
            get {
                return array[index];
            }
            set {
                array[index] = value;
            }
        }

        public int Length {
            get { return array.Length; }
        }

        public void Dispose() {
            // do nothing
        }
    }
    
    struct HybridNativeArray<T> : IDisposable where T: struct 
    {
        NativeArray<T> array;

        public HybridNativeArray(int size) {
            array = new NativeArray<T>(1, Allocator.Persistent);
        }
	    
        public T this[int index]
        {
            get {
                return array[index];
            }
            set {
                array[index] = value;
            }
        }

        public int Length {
            get { return array.Length; }
        }
        
        public void Dispose() {
            array.Dispose();
        }
    }

    class TestHybridArray : ECSLeakTestsFixture
    {
        [Test]
        public void testHybridArray() {
            // --- common array usage
            byte[] array = new byte[1];
            byte[] array2 = new byte[1];
            int l0 = array.Length;
            array2[0] = 1;
            
        //  array.Dispose();
            array = array2;
            AreEqual(1, array[0]);
        //  array.Dispose();

            // --- usage of encapsulated managed array 
            HybridArray<byte> hybrid = new HybridArray<byte>(1);
            HybridArray<byte> hybrid2 = new HybridArray<byte>(1);
            int l1 = hybrid.Length;
            hybrid2[0] = 1;
            
            hybrid.Dispose();
            hybrid = hybrid2;
            AreEqual(1, hybrid[0]);
            hybrid.Dispose();

            // --- usage of encapsulated NativeArray
            HybridNativeArray<byte> hybridNative = new HybridNativeArray<byte>(1);
            HybridNativeArray<byte> hybridNative2 = new HybridNativeArray<byte>(1);
            int l3 = hybridNative.Length;
            hybridNative2[0] = 1;
            
            hybridNative.Dispose();
            hybridNative = hybridNative2;
            AreEqual(1, hybridNative[0]);
            hybridNative.Dispose();
        }

        [Test]
        public void testNativeArrayBehavior() {
            NativeArray<byte> array = new NativeArray<byte>(1, Allocator.Persistent);
            array.Dispose();
            var ex = Assert.Throws<InvalidOperationException>(() =>
                array.Dispose() // second Dispose() throw an exception
            );
            AreEqual(typeof(InvalidOperationException), ex.GetType());
            AreEqual("The Unity.Collections.NativeArray`1[System.Byte] has been deallocated, it is not allowed to access it", ex.Message);
            
            array = new NativeArray<byte>(1, Allocator.Persistent); // assigning to a deallocated NativeArray is valid
            NativeArray<byte> temp = array;
            array = new NativeArray<byte>(2, Allocator.Persistent); // assigning to an already assigned NativeArray is valid, but may cause a leak
            temp.Dispose();
            array.Dispose();
        }
    }
}

#endif // UNITY_5_3_OR_NEWER
