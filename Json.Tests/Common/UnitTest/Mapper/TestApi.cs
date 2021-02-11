using System.IO;
using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestApi
    {
        [Test]
        public void ReadWriteBytes() {
            // Ensure existence of basic API methods
            using (TypeStore typeStore = new TypeStore())
            using (JsonReader read = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter write = new JsonWriter(typeStore))
            
            using (var num1 = new Bytes("1"))
            using (var arr1 = new Bytes("[1]"))
            {
                // --- Read ---
                // 1.
                AreEqual(1, read.Read<int>(num1));                      // generic
                
                // 2.
                AreEqual(1, read.ReadObject(num1, typeof(int)));        // non generic
                
                
                // --- Write ---
                // 1.
                write.Write(1);                                         // generic
                AreEqual("1", write.bytes.ToString());
                
                // 2.
                write.WriteObject(1);                                   // non generic 
                AreEqual("1", write.bytes.ToString());
                

                // --- ReadTo ---
                int[] reuse  = new int[1];
                int[] expect = { 1 };
                int[] result = read.ReadTo(arr1, reuse);                // generic
                AreEqual(expect, result);   
                IsTrue(reuse == result); // same reference - size dit not change
                
                object resultObj = read.ReadObjectTo(arr1, reuse);      // non generic
                AreEqual(expect, resultObj);
                IsTrue(reuse == resultObj); // same reference - size dit not change
            }
        }
        
        [Test]
        public void ReadWriteStream() {
            // Ensure existence of basic API methods
            using (TypeStore typeStore = new TypeStore())
            using (JsonReader read = new JsonReader(typeStore, JsonReader.NoThrow))
            {
                // --- Read ---
                // 1.
                AreEqual(1, read.Read<int>(StreamFromString("1")));                 // generic
                
                // 2.
                AreEqual(1, read.ReadObject(StreamFromString("1"), typeof(int)));   // non generic
                
                // --- ReadTo ---
                int[] reuse  = new int[1];
                int[] expect = { 1 };
                int[] result = read.ReadTo(StreamFromString("[1]"), reuse);             // generic
                AreEqual(expect, result);   
                IsTrue(reuse == result); // same reference - size dit not change
                
                object resultObj = read.ReadObjectTo(StreamFromString("[1]"), reuse);   // non generic
                AreEqual(expect, resultObj);
                IsTrue(reuse == resultObj); // same reference - size dit not change
            }
        }
        
        private static Stream StreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        
        [Test]
        public void ReaderException() {
            using (TypeStore typeStore = new TypeStore())
            using (JsonReader read = new JsonReader(typeStore))
            using (var invalid = new Bytes("invalid"))
            {
                var e = Throws<JsonReaderException>(() => read.Read<string>(invalid));
                AreEqual("JsonParser/JSON error: unexpected character while reading value. Found: i path: '(root)' at position: 1", e.Message);
                AreEqual(1, e.position);
            }
        }
        
        [Test]
        public void ReaderError() {
            using (TypeStore typeStore = new TypeStore())
            using (JsonReader read = new JsonReader(typeStore, JsonReader.NoThrow))
            using (var invalid = new Bytes("invalid"))
            {
                read.Read<string>(invalid);
                IsFalse(read.Success);
                AreEqual("JsonParser/JSON error: unexpected character while reading value. Found: i path: '(root)' at position: 1", read.Error.msg.ToString());
                AreEqual(1, read.Error.Pos);
            }
        }
    }
}