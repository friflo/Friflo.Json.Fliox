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
            array.WriteNull();
            array.WriteBoolean  (true);
            array.WriteByte     (255);
            array.WriteInt16    (short.MaxValue);
            array.WriteInt32    (int.MaxValue);
            array.WriteInt64    (long.MaxValue);
            
            array.WriteFlt32    (float.MaxValue);
            array.WriteFlt64    (double.MaxValue);

            array.WriteBytes    (Bytes.AsSpan());
            array.WriteChars    ("test".AsSpan());
            array.WriteChars    ("chars".AsSpan());
            array.WriteDateTime (DateTime);
            array.WriteGuid     (Guid);
            array.Finish        ();
        }
            
        private static void ReadTestData (JsonArray array, ReadArrayType readArrayType)
        {
            int pos         = 0;
            var type        = JsonItemType.Null;
            int stringCount = 0;
            while (type != JsonItemType.End)
            {
                type = array.GetItemType(pos, out int next);
                switch (type) {
                    case JsonItemType.Null:
                        break;
                    case JsonItemType.True:
                    case JsonItemType.False: {
                        var value = array.ReadBool(pos);
                        IsTrue(value);
                        break;
                    }
                    case JsonItemType.Uint8: {
                        var value = array.ReadUint8(pos);
                        AreEqual(255, value);
                        break;
                    }
                    case JsonItemType.Int16: {
                        var value = array.ReadInt16(pos);
                        AreEqual(short.MaxValue, value);
                        break;
                    }
                    case JsonItemType.Int32: {
                        var value = array.ReadInt32(pos);
                        AreEqual(int.MaxValue, value);
                        break;
                    }
                    case JsonItemType.Int64: {
                        var value = array.ReadInt64(pos);
                        AreEqual(long.MaxValue, value);
                        break;
                    }
                    case JsonItemType.Flt32: {
                        var value = array.ReadFlt32(pos);
                        AreEqual(float.MaxValue, value);
                        break;
                    }
                    case JsonItemType.Flt64: {
                        var value = array.ReadFlt64(pos);
                        AreEqual(double.MaxValue, value);
                        break;
                    }
                    case JsonItemType.ByteString: {
                        var value = array.ReadBytesSpan(pos);
                        if (readArrayType == ReadArrayType.Binary) {
                            IsTrue(value.SequenceEqual(Bytes.AsSpan()));
                        } else {
                            switch (stringCount++) {
                                case 0: IsTrue(value.SequenceEqual(Bytes.AsSpan()));                break;
                                case 1: IsTrue(value.SequenceEqual(new Bytes("test").AsSpan()));    break;
                                case 2: IsTrue(value.SequenceEqual(new Bytes("chars").AsSpan()));   break;
                                default: Fail(); break;
                            }
                        }
                        break;
                    }
                    case JsonItemType.CharString:
                        switch (stringCount++) {
                            case 0: {
                                    var value = array.ReadString(pos);
                                    AreEqual("test", value);
                                }
                                break;
                            case 1: {
                                    var value = array.ReadCharSpan(pos);
                                    IsTrue(value.SequenceEqual("chars".AsSpan()));
                                }
                                break;
                            default:
                                Fail(); break;
                        }
                        break;
                    case JsonItemType.DateTime: {
                        var value = array.ReadDateTime(pos);
                        AreEqual(DateTime, value);
                        break;
                    }
                    case JsonItemType.Guid: {
                        var value = array.ReadGuid(pos);
                        AreEqual(Guid, value);
                        break;
                    }
                }
                pos = next;
            }
            AreEqual(true, true);
        }
        
        [Test]
        public static void TestJsonArray_ReadWrite ()
        {
            var array = new JsonArray();
            WriteTestData(array);
            array.ToString();
            AreEqual(13, array.Count);
            ReadTestData(array, ReadArrayType.Binary);
        }

        private const string Expect = "[null,true,255,32767,2147483647,9223372036854775807,3.4028235E+38,1.7976931348623157E+308,\"bytes\",\"test\",\"chars\",\"2023-07-19T12:58:57.448575Z\",\"af82dcf5-8664-4b4e-8072-6cb43b335364\"]";

        [Test]
        public static void TestJsonArray_MapperWrite () 
        {
            var typeStore = new TypeStore();
            var mapper = new ObjectMapper(typeStore);
            
            var array = new JsonArray();
            
            var json = mapper.Write(array);
            AreEqual("[]", json);

            array.Init();
            WriteTestData(array);
            json = mapper.Write(array);
            AreEqual(Expect, json);
        }
        
        [Test]
        public static void TestJsonArray_MapperRead () 
        {
            var typeStore = new TypeStore();
            var mapper = new ObjectMapper(typeStore);

            var array = mapper.Read<JsonArray>(Expect);
            ReadTestData(array, ReadArrayType.Json);
            AreEqual(13, array.Count);
        }
    }
}