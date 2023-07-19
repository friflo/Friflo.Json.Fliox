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
        
        // -------------------------------------------- write --------------------------------------------
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
        
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
        
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
        
        
        // -------------------------------------------- read --------------------------------------------
        public JsonItemType GetItemType(int pos, out int next) {
            var type = (JsonItemType)bytes.buffer[pos];
            switch (type) {
                case JsonItemType.Null:
                case JsonItemType.True:
                case JsonItemType.False:
                case JsonItemType.End:
                    next = pos + 1;
                    return type;
                case JsonItemType.Uint8:
                    next = pos + 2;
                    return type;
                case JsonItemType.Int16:
                    next = pos + 3;
                    return type;
                case JsonItemType.Int32:
                case JsonItemType.Flt32:
                    next = pos + 5;
                    return type;
                case JsonItemType.DateTime:
                case JsonItemType.Int64:
                case JsonItemType.Flt64:
                    next = pos + 9;
                    return type;
                case JsonItemType.Guid:
                    next = pos + 17;
                    return type;
                case JsonItemType.Chars:
                    next = pos + 1 + 4 + bytes.ReadInt32(pos + 1); 
                    return type;
                default:
                    throw new InvalidOperationException($"unexpected type: {type}");
            }
        }
        
        public bool ReadBool(int pos) {
            return bytes.buffer[pos] == (byte)JsonItemType.True;
        }
        
        public byte ReadUint8(int pos) {
            return bytes.buffer[pos + 1];
        }
        
        public short ReadInt16(int pos) {
            var buffer = bytes.buffer;
            return (short)(buffer[pos + 1] << 8 | buffer[pos + 2]);
        }
        
        public int ReadInt32(int pos) {
            return bytes.ReadInt32(pos + 1);
        }
        
        public long ReadInt64(int pos) {
            return bytes.ReadInt64(pos + 1);
        }
        
        public float ReadFlt32(int pos) {
            return bytes.ReadFlt32(pos + 1);
        }
        
        public double ReadFlt64(int pos) {
            return bytes.ReadFlt64(pos + 1);
        }
        
        public string ReadString(int pos) {
            var len = bytes.ReadInt32(pos + 1);
            return Utf8.GetString(bytes.buffer, pos + 1 + 4, len);
        }
        
        public ReadOnlySpan<char> ReadCharSpan(int pos) {
            var len = bytes.ReadInt32(pos + 1);
            return Utf8.GetChars(bytes.buffer, pos + 1 + 4, len);
        }

        
        public DateTime ReadDateTime(int pos) {
            var lng = bytes.ReadInt64(pos + 1);
            return DateTime.FromBinary(lng);
        }
        
        public Guid ReadGuid(int pos) {
            var lng1 = bytes.ReadInt64(pos + 1);
            var lng2 = bytes.ReadInt64(pos + 9);
            return GuidUtils.LongLongToGuid(lng1, lng2);
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