// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox
{
    public class JsonArray
    {
        internal Bytes bytes = new Bytes(32);
        
        public void WriteNull() {
            bytes.EnsureCapacity(1);
            bytes.buffer[bytes.end++] = (byte)JsonItemType.Null;
        }
        
        public void WriteBoolean(bool value) {
            bytes.EnsureCapacity(1);
            bytes.buffer[bytes.end++] = (byte)(value ? JsonItemType.True : JsonItemType.False);
        }

        public void WriteByte(byte value) {
            bytes.EnsureCapacity(2);
            bytes.buffer[bytes.end++] = (byte)JsonItemType.Uint8;
            bytes.buffer[bytes.end++] = value;
        }

        public void WriteInt16(short value) {
            bytes.EnsureCapacity(3);
            var pos = bytes.end;
            bytes.buffer[pos    ] = (byte)JsonItemType.Int16;
            bytes.buffer[pos + 1] = (byte)(value >> 8);
            bytes.buffer[pos + 2] = (byte)(value & 0xff);
            bytes.end += 3;
        }

        public void WriteInt32(int value) {
            bytes.EnsureCapacity(5);
            bytes.buffer[bytes.end] = (byte)JsonItemType.Int32;
            bytes.WriteInt32(bytes.end + 1, value);
            bytes.end += 5;
        }

        public void WriteInt64(long value) {
            bytes.EnsureCapacity(9);
            bytes.buffer[bytes.end] = (byte)JsonItemType.Int64;
            bytes.WriteInt64(bytes.end + 1, value);
            bytes.end += 9;
        }
        
        public void WriteFlt32(float value) {
            bytes.EnsureCapacity(5);
            bytes.buffer[bytes.end] = (byte)JsonItemType.Flt32;
            bytes.WriteFlt32(bytes.end + 1, value);
            bytes.end += 5;
        }
        
        public void WriteFlt64(double value) {
            bytes.EnsureCapacity(9);
            bytes.buffer[bytes.end] = (byte)JsonItemType.Flt64;
            bytes.WriteFlt64(bytes.end + 1, value);
            bytes.end += 9;
        }
        
        internal static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
        
        public void WriteChars(ReadOnlySpan<char> value) {
            if (value == null) {
                WriteNull();
                return;
            }
            var end = bytes.end;
            int maxByteLen = end + Utf8.GetMaxByteCount(value.Length) + 1 + 4;
            if (maxByteLen > bytes.buffer.Length) {
                bytes.DoubleSize(maxByteLen);
            }
            var buffer      = bytes.buffer;
            buffer[end]     = (byte)JsonItemType.Chars;
            var targetStart = end + 1 + 4;
            var target      = new Span<byte> (buffer, targetStart, buffer.Length - targetStart);
            int byteLen     = Utf8.GetBytes(value, target);
            bytes.WriteInt32(end + 1, byteLen);
            bytes.end += 1 + 4 + byteLen;
        }

        public void WriteDateTime(in DateTime value) {
            bytes.EnsureCapacity(9);
            bytes.buffer[bytes.end] = (byte)JsonItemType.DateTime;
            bytes.WriteInt64(bytes.end + 1, value.ToBinary());
            bytes.end += 9;
        }
        
        public void WriteGuid(in Guid value) {
            bytes.EnsureCapacity(17);
            bytes.buffer[bytes.end] = (byte)JsonItemType.Guid;
            GuidUtils.GuidToLongLong(value, out long value1, out long value2);
            bytes.WriteLongLong(bytes.end + 1, value1, value2);
            bytes.end += 17;
        }
        
        public void Finish() {
            bytes.EnsureCapacity(1);
            bytes.buffer[bytes.end] = (byte)JsonItemType.End;
        }
    }
    
    public enum JsonItemType
    {
        Null        =  0,
        //
        True        =  1,
        False       =  2,
        // --- integer
        Uint8       =  3,
        Int16       =  4,
        Int32       =  5,
        Int64       =  6,
        //
        Flt32       =  7,
        Flt64       =  8,
        //
        Chars       =  9,
        DateTime    = 10,
        Guid        = 11,
        
        End         = 12,
    }
}