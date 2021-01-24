using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestApi
    {
        [Test]
        public void Run() {
            // Ensure existence of basic API methods
            using (TypeStore typeStore = new TypeStore())
            using (JsonReader read = new JsonReader(typeStore))
            using (JsonWriter write = new JsonWriter(typeStore))
            
            using (var num1 = new Bytes("1"))
            using (var arr1 = new Bytes("[1]"))
            {
                // --- Read ---
                // 1. a
                AreEqual(1, read.Read<int>(num1));                      // generic
                
                // 1. b
                AreEqual(1, read.Read<int>(num1, out bool success));    // generic
                IsTrue(success);

                // 2.
                // AreEqual(1,     read.Read(num1, typeof(int)));       // non generic
                
                
                // --- Write ---
                // 1.
                write.Write(1);                                         // generic
                AreEqual("1", write.bytes.ToString());
                
                // 2.
                write.Write((object) 1);                                // non generic
                AreEqual("1", write.bytes.ToString());
                

                // --- ReadTo ---
                int[] arr       = new int[1];
                int[] expect    = new int[1];
                AreEqual(expect, read.ReadTo(arr1, arr, out bool _));   // generic
            }
        }
    }
}