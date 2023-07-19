// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Base
{
    public static class TestJsonArray
    {
        [Test]
        public static void JsonKeyTests () {
            var array       = new JsonArray();
            var dateTime    = DateTime.Now;
            var guid        = Guid.NewGuid();
            
            array.WriteNull();
            array.WriteBoolean  (true);
            array.WriteByte     (255);
            array.WriteInt16    (short.MaxValue);
            array.WriteInt32    (int.MaxValue);
            array.WriteInt64    (long.MaxValue);
            
            array.WriteFlt32    (float.MaxValue);
            array.WriteFlt64    (double.MaxValue);

            array.WriteChars    ("test".AsSpan());
            array.WriteChars    ("chars".AsSpan());
            array.WriteDateTime (dateTime);
            array.WriteGuid     (guid);
            array.Finish        ();
            
            
            var reader = new JsonArrayReader();
            reader.Init(array);
            int pos         = 0;
            var type        = JsonItemType.Null;
            int stringCount = 0;
            while (type != JsonItemType.End)
            {
                type = reader.GetItemType(pos, out int next);
                switch (type) {
                    case JsonItemType.Null:
                        break;
                    case JsonItemType.True:
                    case JsonItemType.False: {
                        var value = reader.ReadBool(pos);
                        IsTrue(value);
                        break;
                    }
                    case JsonItemType.Uint8: {
                        var value = reader.ReadUint8(pos);
                        AreEqual(255, value);
                        break;
                    }
                    case JsonItemType.Int16: {
                        var value = reader.ReadInt16(pos);
                        AreEqual(short.MaxValue, value);
                        break;
                    }
                    case JsonItemType.Int32: {
                        var value = reader.ReadInt32(pos);
                        AreEqual(int.MaxValue, value);
                        break;
                    }
                    case JsonItemType.Int64: {
                        var value = reader.ReadInt64(pos);
                        AreEqual(long.MaxValue, value);
                        break;
                    }
                    case JsonItemType.Flt32: {
                        var value = reader.ReadFlt32(pos);
                        AreEqual(float.MaxValue, value);
                        break;
                    }
                    case JsonItemType.Flt64: {
                        var value = reader.ReadFlt64(pos);
                        AreEqual(double.MaxValue, value);
                        break;
                    }
                    case JsonItemType.Chars: {
                        if (stringCount++ == 0) { 
                            var value = reader.ReadString(pos);
                            AreEqual("test", value);
                        } else {
                            var value = reader.ReadCharSpan(pos);
                            IsTrue(value.SequenceEqual("chars".AsSpan()));
                        }
                        break;
                    }
                    case JsonItemType.DateTime: {
                        var value = reader.ReadDateTime(pos);
                        AreEqual(dateTime, value);
                        break;
                    }
                    case JsonItemType.Guid: {
                        var value = reader.ReadGuid(pos);
                        AreEqual(guid, value);
                        break;
                    }
                }
                pos = next;
            }

            AreEqual(true, true);
        }
    }
}