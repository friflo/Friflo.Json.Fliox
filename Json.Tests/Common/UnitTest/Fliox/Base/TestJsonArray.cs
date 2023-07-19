// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Base
{
    internal enum ReadArrayType {
        Binary,
        Json
    }
        
    public static class TestJsonArray
    {
        private static readonly DateTime    DateTime    = DateTime.Parse("2023-07-19T12:58:57.448575Z").ToUniversalTime();
        private static readonly Guid        Guid        = Guid.Parse("af82dcf5-8664-4b4e-8072-6cb43b335364");
        private static readonly Bytes       Bytes       = new Bytes("bytes");
        
        
        private static void WriteTestData (JsonArray array) {
            array.WriteNull();                      // [0]
            array.WriteBoolean  (true);             // [1]
            array.WriteByte     (255);              // [2]
            array.WriteInt16    (short.MaxValue);   // [3]
            array.WriteInt32    (int.MaxValue);     // [4]
            array.WriteInt64    (long.MaxValue);    // [5]
            
            array.WriteFlt32    (float.MaxValue);   // [6]  3.4028235E+38f;
            array.WriteFlt64    (double.MaxValue);  // [7]  1.7976931348623157E+308;

            array.WriteBytes    (Bytes.AsSpan());   // [8]
            array.WriteChars    ("test".AsSpan());  // [9]
            array.WriteChars    ("chars".AsSpan()); // [10]
            array.WriteDateTime (DateTime);         // [11]
            array.WriteGuid     (Guid);             // [12]
            array.Finish        (); // TODO remove
        }
            
        private static void ReadTestData (JsonArray array, ReadArrayType readArrayType)
        {
            int pos         = 0;
            var idx     = new int[13];
            int n           = 0;
            while (true) {
                var type = array.GetItemType(pos, out int next);
                if (type == JsonItemType.End) {
                    break;
                }
                idx[n++] = pos;
                pos = next;
            }
            AreEqual(0,                                      idx[0]); // null
            IsTrue(                     array.ReadBool      (idx[1]));
            AreEqual(255,               array.ReadUint8     (idx[2]));
            AreEqual(short.MaxValue,    array.ReadInt16     (idx[3]));
            AreEqual(int.MaxValue,      array.ReadInt32     (idx[4]));
            AreEqual(long.MaxValue,     array.ReadInt64     (idx[5]));
#if !UNITY_5_3_OR_NEWER
            AreEqual(float.MaxValue,    array.ReadFlt32     (idx[6]));
            AreEqual(double.MaxValue,   array.ReadFlt64     (idx[7]));
#endif
            var bytes =                 array.ReadBytesSpan (idx[8]);
            IsTrue(bytes.SequenceEqual(Bytes.AsSpan()));
            
            if (readArrayType == ReadArrayType.Binary) {
                var chars =             array.ReadCharSpan  (idx[9]);
                IsTrue(chars.SequenceEqual("test".AsSpan()));
                
                chars =                 array.ReadCharSpan  (idx[10]);
                IsTrue(chars.SequenceEqual("chars".AsSpan()));
            } else {
                bytes =                 array.ReadBytesSpan (idx[9]);
                IsTrue(bytes.SequenceEqual(new Bytes("test").AsSpan()));
                
                bytes =                 array.ReadBytesSpan (idx[10]);
                IsTrue(bytes.SequenceEqual(new Bytes("chars").AsSpan()));
            }
            
            AreEqual(DateTime,          array.ReadDateTime  (idx[11]));
            AreEqual(Guid,              array.ReadGuid      (idx[12]));
        }
        
        [Test]
        public static void TestJsonArray_ReadWrite ()
        {
            var array = new JsonArray();
            WriteTestData(array);
            AreEqual(13, array.Count);
            ReadTestData(array, ReadArrayType.Binary);
        }

        // Note! Unity format floating point numbers with lower precision
        private const string ExpectJson =
            "[null,true,255,32767,2147483647,9223372036854775807,3.4028235E+38,1.7976931348623157E+308,\"bytes\",\"test\",\"chars\",\"2023-07-19T12:58:57.448575Z\",\"af82dcf5-8664-4b4e-8072-6cb43b335364\"]";
        
        private const string ExpectToString =
            "Count: 13 [null, true, 255, 32767, 2147483647, 9223372036854775807, 3.4028235E+38, 1.7976931348623157E+308, 'bytes', 'test', 'chars', 2023-07-19 12:58:57, af82dcf5-8664-4b4e-8072-6cb43b335364]";


        [Test]
        public static void TestJsonArray_MapperWrite () 
        {
            var typeStore = new TypeStore();
            var mapper = new ObjectMapper(typeStore);
            
            var array = new JsonArray();
            AreEqual("Count: 0 []", array.ToString());
            
            var json = mapper.Write(array);
            AreEqual("[]", json);

            array.Init();
            WriteTestData(array);
            json = mapper.Write(array);
            var toString = array.ToString();
#if !UNITY_5_3_OR_NEWER
            AreEqual(ExpectToString, toString);
            AreEqual(ExpectJson, json);
#endif
        }
        
        [Test]
        public static void TestJsonArray_MapperRead () 
        {
            var typeStore = new TypeStore();
            var mapper = new ObjectMapper(typeStore);

            var array = mapper.Read<JsonArray>(ExpectJson);
            ReadTestData(array, ReadArrayType.Json);
            AreEqual(13, array.Count);
        }
        
        [Test]
        public static void TestJsonArray_ReadErrors () 
        {
            var typeStore = new TypeStore();
            var mapper = new ObjectMapper(typeStore);

            var e = Throws<JsonReaderException>(() => {
                 mapper.Read<JsonArray>("[123-]");
            });
            IsNotNull(e);
            AreEqual("JsonReader/error: invalid integer: 123- path: '[0]' at position: 5", e.Message);

            e = Throws<JsonReaderException>(() => {
                mapper.Read<JsonArray>("[123e+38.999]");
            });
            IsNotNull(e);
            AreEqual("JsonReader/error: invalid floating point number: 123e+38.999 path: '[0]' at position: 12", e.Message);
        }
    }
}