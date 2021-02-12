using System.IO;
using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

using static NUnit.Framework.Assert;
using static Friflo.Json.Mapper.Convert;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    // Ensure existence of basic API methods
    public class TestApi
    {
        // ------------------------------------ JsonReader / JsonWriter ------------------------------------
        [Test]
        public void ReadWriteBytes() {
            using (TypeStore    typeStore   = new TypeStore())
            using (JsonReader   read        = new JsonReader(typeStore))
            using (JsonWriter   write       = new JsonWriter(typeStore))
            
            using (var num1 = new Bytes("1"))
            using (var arr1 = new Bytes("[1]"))
            using (var dst  = new TestBytes())
            {
                // --- Read ---
                AreEqual(1, read.Read<int>(num1));                      // generic
                
                AreEqual(1, read.ReadObject(num1, typeof(int)));        // non generic
                
                
                // --- Write ---
                write.Write(1, ref dst.bytes);                          // generic
                AreEqual("1", dst.bytes.ToString());
                
                write.WriteObject(1, ref dst.bytes);                    // non generic 
                AreEqual("1", dst.bytes.ToString());
                

                // --- ReadTo ---
                int[] reuse  = new int[1];
                int[] expect = { 1 };
                int[] result = read.ReadTo(arr1, reuse);                // generic
                AreEqual(expect, result);   
                IsTrue(reuse == result); // same reference - size did not change
                
                object resultObj = read.ReadToObject(arr1, reuse);      // non generic
                AreEqual(expect, resultObj);
                IsTrue(reuse == resultObj); // same reference - size did not change
            }
        }
        
        [Test]
        public void ReadWriteStream() {
            using (TypeStore    typeStore   = new TypeStore())
            using (JsonReader   read        = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter   write       = new JsonWriter(typeStore))
            {
                // --- Read ---
                AreEqual(1, read.Read<int>(StreamFromString("1")));                     // generic
                
                AreEqual(1, read.ReadObject(StreamFromString("1"), typeof(int)));       // non generic
                
#if !JSON_BURST
                // --- Write ---
                Stream stream = new MemoryStream();
                write.Write(1, stream);                                                 // generic
                AreEqual("1", StringFromStream(stream));

                stream.Position = 0;
                write.WriteObject(1, stream);                                           // non generic 
                AreEqual("1", StringFromStream(stream));
#endif
                
                // --- ReadTo ---
                int[] reuse  = new int[1];
                int[] expect = { 1 };
                int[] result = read.ReadTo(StreamFromString("[1]"), reuse);             // generic
                AreEqual(expect, result);   
                IsTrue(reuse == result); // same reference - size did not change
                
                object resultObj = read.ReadToObject(StreamFromString("[1]"), reuse);   // non generic
                AreEqual(expect, resultObj);
                IsTrue(reuse == resultObj); // same reference - size did not change
            }
        }
        
        [Test]
        public void ReadWriteString() {
            using (TypeStore    typeStore   = new TypeStore())
            using (JsonReader   read        = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter   write       = new JsonWriter(typeStore))
            {
                // --- Read ---
                AreEqual(1, read.Read<int>("1"));                       // generic
                
                AreEqual(1, read.ReadObject("1", typeof(int)));         // non generic
                
                
                // --- Write ---
                var json1 = write.Write(1);                             // generic
                AreEqual("1", json1);
                
                var json2 = write.WriteObject(1);                       // non generic 
                AreEqual("1", json2);

                
                // --- ReadTo ---
                int[] reuse  = new int[1];
                int[] expect = { 1 };
                int[] result = read.ReadTo("[1]", reuse);               // generic
                AreEqual(expect, result);   
                IsTrue(reuse == result); // same reference - size did not change
                
                object resultObj = read.ReadToObject("[1]", reuse);     // non generic
                AreEqual(expect, resultObj);
                IsTrue(reuse == resultObj); // same reference - size did not change
            }
        }
        
        // --------------------------------------- JSON ---------------------------------------
        [Test]
        public void JsonBytes() {
            using (var num1 = new Bytes("1"))
            using (var arr1 = new Bytes("[1]"))
            using (var dst  = new TestBytes())
            {
                // --- Read ---
                AreEqual(1, JSON.Read<int>(num1));                      // generic
                
                AreEqual(1, JSON.ReadObject(num1, typeof(int)));        // non generic
                
                
                // --- Write ---
                JSON.Write(1, ref dst.bytes);                           // generic
                AreEqual("1", dst.bytes.ToString());
                
                JSON.WriteObject(1, ref dst.bytes);                     // non generic 
                AreEqual("1", dst.bytes.ToString());

                
                // --- ReadTo ---
                int[] reuse  = new int[1];
                int[] expect = { 1 };
                int[] result = JSON.ReadTo(arr1, reuse);                // generic
                AreEqual(expect, result);   
                IsTrue(reuse == result); // same reference - size did not change
                
                object resultObj = JSON.ReadToObject(arr1, reuse);      // non generic
                AreEqual(expect, resultObj);
                IsTrue(reuse == resultObj); // same reference - size did not change
            }
        }
        
        [Test]
        public void JsonStream() {
            // --- Read ---
            AreEqual(1, JSON.Read<int>(StreamFromString("1")));                    // generic
            
            AreEqual(1, JSON.ReadObject(StreamFromString("1"), typeof(int)));      // non generic
            
#if !JSON_BURST
            // --- Write ---
            Stream stream = new MemoryStream();
            JSON.Write(1, stream);                                                 // generic
            AreEqual("1", StringFromStream(stream));

            stream.Position = 0;
            JSON.WriteObject(1, stream);                                           // non generic 
            AreEqual("1", StringFromStream(stream));
#endif
            
            // --- ReadTo ---
            int[] reuse  = new int[1];
            int[] expect = { 1 };
            int[] result = JSON.ReadTo(StreamFromString("[1]"), reuse);             // generic
            AreEqual(expect, result);   
            IsTrue(reuse == result); // same reference - size did not change
            
            object resultObj = JSON.ReadToObject(StreamFromString("[1]"), reuse);   // non generic
            AreEqual(expect, resultObj);
            IsTrue(reuse == resultObj); // same reference - size did not change
        }
        
        [Test]
        public void JsonString() {
            // --- Read ---
            AreEqual(1, JSON.Read<int>("1"));                       // generic
            
            AreEqual(1, JSON.ReadObject("1", typeof(int)));         // non generic
            
            // --- Write ---
            var json1 = JSON.Write(1);                              // generic
            AreEqual("1", json1);
                
            var json2 = JSON.WriteObject(1);                        // non generic 
            AreEqual("1", json2);
            
            // --- ReadTo ---
            int[] reuse  = new int[1];
            int[] expect = { 1 };
            int[] result = JSON.ReadTo("[1]", reuse);               // generic
            AreEqual(expect, result);   
            IsTrue(reuse == result); // same reference - size did not change
            
            object resultObj = JSON.ReadToObject("[1]", reuse);     // non generic
            AreEqual(expect, resultObj);
            IsTrue(reuse == resultObj); // same reference - size did not change
        }
        
        // --------------------------------------- Formatter ---------------------------------------
        [Test]
        public void FormatterBytes() {
            using (var  formatter   = new JsonMapper())
            using (var  num1        = new Bytes("1"))
            using (var  arr1        = new Bytes("[1]"))
            using (var  dst         = new TestBytes())
            {
                // --- Read ---
                AreEqual(1, formatter.Read<int>(num1));                     // generic
                
                AreEqual(1, formatter.ReadObject(num1, typeof(int)));       // non generic
                

                // --- Write ---
                formatter.Write(1, ref dst.bytes);                          // generic
                AreEqual("1", dst.bytes.ToString());
                
                formatter.WriteObject(1, ref dst.bytes);                    // non generic 
                AreEqual("1", dst.bytes.ToString());


                // --- ReadTo ---
                int[] reuse  = new int[1];
                int[] expect = { 1 };
                int[] result = formatter.ReadTo(arr1, reuse);               // generic
                AreEqual(expect, result);   
                IsTrue(reuse == result); // same reference - size did not change
                
                object resultObj = formatter.ReadToObject(arr1, reuse);     // non generic
                AreEqual(expect, resultObj);
                IsTrue(reuse == resultObj); // same reference - size did not change
            }
        }
        
        [Test]
        public void FormatterStream() {
            using (var formatter = new JsonMapper()) {
                // --- Read ---
                AreEqual(1, formatter.Read<int>(StreamFromString("1")));                    // generic
                
                AreEqual(1, formatter.ReadObject(StreamFromString("1"), typeof(int)));      // non generic
                
#if !JSON_BURST
                // --- Write ---
                Stream stream = new MemoryStream();
                formatter.Write(1, stream);                                                 // generic
                AreEqual("1", StringFromStream(stream));

                stream.Position = 0;
                formatter.WriteObject(1, stream);                                           // non generic 
                AreEqual("1", StringFromStream(stream));
#endif
                
                // --- ReadTo ---
                int[] reuse  = new int[1];
                int[] expect = { 1 };
                int[] result = formatter.ReadTo(StreamFromString("[1]"), reuse);            // generic
                AreEqual(expect, result);   
                IsTrue(reuse == result); // same reference - size did not change
                
                object resultObj = formatter.ReadToObject(StreamFromString("[1]"), reuse);  // non generic
                AreEqual(expect, resultObj);
                IsTrue(reuse == resultObj); // same reference - size did not change
            }
        }
        
        [Test]
        public void FormatterString() {
            using (var formatter = new JsonMapper()) {
                // --- Read ---
                AreEqual(1, formatter.Read<int>("1")); // generic

                AreEqual(1, formatter.ReadObject("1", typeof(int))); // non generic
                
                
                // --- Write ---
                var json1 = formatter.Write(1);                              // generic
                AreEqual("1", json1);
                
                var json2 = formatter.WriteObject(1);                        // non generic 
                AreEqual("1", json2);

                // --- ReadTo ---
                int[] reuse = new int[1];
                int[] expect = {1};
                int[] result = formatter.ReadTo("[1]", reuse); // generic
                AreEqual(expect, result);
                IsTrue(reuse == result); // same reference - size did not change

                object resultObj = formatter.ReadToObject("[1]", reuse); // non generic
                AreEqual(expect, resultObj);
                IsTrue(reuse == resultObj); // same reference - size did not change
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
        
        private static string StringFromStream(Stream stream)
        {
            stream.Position = 0;
            StreamReader reader = new StreamReader(stream);
            string str = reader.ReadToEnd();
            return str;
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