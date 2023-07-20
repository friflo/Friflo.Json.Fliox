// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox
{
    public class JsonArray
    {
        public          int     Count       => count;
        public override string  ToString()  => AsString();

        // --- private
        private Bytes   bytes;
        private int     count;
        
        public JsonArray() {
            bytes = new Bytes(32);
        }
        
        public JsonArray(int count, JsonArray array, int start, int end) {
            this.count      = count;           
            bytes.buffer    = array.bytes.buffer;
            bytes.start     = start;
            bytes.end       = end;
        }
        
        public void Init() {
            count       = 0;
            bytes.start = 0;
            bytes.end   = 0;
        }
        
        // -------------------------------------------- write --------------------------------------------
        public void WriteNull() {
            bytes.EnsureCapacity(1);
            bytes.buffer[bytes.end++] = (byte)JsonItemType.Null;
            count++;
        }
        
        public void WriteBoolean(bool value) {
            bytes.EnsureCapacity(1);
            bytes.buffer[bytes.end++] = (byte)(value ? JsonItemType.True : JsonItemType.False);
            count++;
        }

        public void WriteByte(byte value) {
            bytes.EnsureCapacity(2);
            int start = bytes.end;
            bytes.buffer[start]     = (byte)JsonItemType.Uint8;
            bytes.buffer[start + 1] = value;
            bytes.end = start + 2;
            count++;
        }

        public void WriteInt16(short value) {
            if (value > byte.MaxValue || value < byte.MinValue) {
                bytes.EnsureCapacity(3);
                var start = bytes.end;
                bytes.buffer[start    ] = (byte)JsonItemType.Int16;
                bytes.buffer[start + 1] = (byte)(value >> 8);
                bytes.buffer[start + 2] = (byte)(value & 0xff);
                bytes.end = start + 3;
                count++;
                return;
            }
            WriteByte((byte)value);
        }

        public void WriteInt32(int value) {
            if (value > short.MaxValue || value < short.MinValue) {
                bytes.EnsureCapacity(5);
                var start = bytes.end;
                bytes.buffer[start] = (byte)JsonItemType.Int32;
                bytes.WriteInt32(start + 1, value);
                bytes.end = start + 5;
                count++;
                return;
            }
            WriteInt16((short)value);
        }

        public void WriteInt64(long value) {
            if (value > int.MaxValue || value < int.MinValue) {
                bytes.EnsureCapacity(9);
                var start = bytes.end;
                bytes.buffer[start] = (byte)JsonItemType.Int64;
                bytes.WriteInt64(start + 1, value);
                bytes.end = start + 9;
                count++;
                return;
            }
            WriteInt32((int)value);
        }
        
        public void WriteFlt32(float value) {
            bytes.EnsureCapacity(5);
            var start = bytes.end;
            bytes.buffer[start] = (byte)JsonItemType.Flt32;
            bytes.WriteFlt32(start + 1, value);
            bytes.end = start + 5;
            count++;
        }
        
        public void WriteFlt64(double value) {
            bytes.EnsureCapacity(9);
            var start = bytes.end;
            bytes.buffer[start] = (byte)JsonItemType.Flt64;
            bytes.WriteFlt64(start + 1, value);
            bytes.end = start + 9;
            count++;
        }
        
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
        
        /// <summary>
        /// <b>Important!</b> <br/>
        /// Passed value MUST be valid JSON. Otherwise invalid JSON will be generated downstream.
        /// </summary>
        public void WriteJSON(ReadOnlySpan<byte> value) {
            count++;
            int len = value.Length;
            bytes.EnsureCapacity(1 + 4 + len);
            int start = bytes.end;
            bytes.buffer[start] = (byte)JsonItemType.JSON;
            bytes.WriteInt32(start + 1, len);
            bytes.end = start + 1 + 4;
            bytes.AppendBytesSpan(value);
        }
        
        /// <summary>
        /// <b>Important!</b> <br/>
        /// Passed value MUST be valid JSON. Otherwise invalid JSON will be generated downstream.<br/>
        /// <b>Note</b> Prefer using <see cref="WriteJSON"/> to avoid char[] -> UTF-8 conversion
        /// </summary>
        public void WriteCharJSON(ReadOnlySpan<char> value) {
            count++;
            var maxLen = Utf8.GetMaxByteCount(value.Length);
            bytes.EnsureCapacity(1 + 4 + maxLen);
            int start       = bytes.end;
            int valueStart  = start + 1 + 4;
            var buffer      = bytes.buffer;
            var target      = new Span<byte> (buffer, valueStart, buffer.Length - valueStart);
            var len         = Utf8.GetBytes(value, target);
            buffer[start] = (byte)JsonItemType.JSON;
            bytes.WriteInt32 (start + 1,      len);
            bytes.end       = start + 1 + 4 + len;
        }
        
        public void WriteCharString(ReadOnlySpan<char> value) {
            if (value == null) {
                WriteNull();
                return;
            }
            count++;
            var len     = value.Length;
            var start   = bytes.end;
            bytes.EnsureCapacity (1 + 4 + 2 * len);
            bytes.buffer        [start]   = (byte)JsonItemType.CharString;
            bytes.WriteInt32    (start + 1,     len); // count chars (not bytes)
            bytes.WriteCharArray(start + 1 + 4, value);
            bytes.end = start + 1 + 4 + 2 * len;
        }
        
        public void WriteByteString(ReadOnlySpan<byte> value) {
            if (value == null) {
                WriteNull();
                return;
            }
            count++;
            var len     = value.Length;
            int start   = bytes.end;
            bytes.EnsureCapacity(1 + 4 + len);
            bytes.buffer[start] = (byte)JsonItemType.ByteString;
            bytes.WriteInt32(start + 1, len);
            bytes.end = start + 1 + 4;
            bytes.AppendBytesSpan(value);
        }
        
        public void WriteDateTime(in DateTime value) {
            count++;
            bytes.EnsureCapacity(9);
            int start = bytes.end;
            bytes.buffer[start] = (byte)JsonItemType.DateTime;
            bytes.WriteInt64(start + 1, value.ToBinary());
            bytes.end = start + 9;
        }
        
        public void WriteGuid(in Guid value) {
            count++;
            bytes.EnsureCapacity(17);
            int start = bytes.end;
            bytes.buffer[start] = (byte)JsonItemType.Guid;
            GuidUtils.GuidToLongLong(value, out long value1, out long value2);
            bytes.WriteLongLong(start + 1, value1, value2);
            bytes.end = start + 17;
        }
        
        // -------------------------------------------- read --------------------------------------------
        public JsonItemType GetItemType(int pos) {
            if (pos < bytes.end) {
                return (JsonItemType)bytes.buffer[pos];
            }
            return JsonItemType.End;
        }
        
        public JsonItemType GetItemType(int pos, out int next) {
            if (pos >= bytes.end) {
                next = pos;
                return JsonItemType.End;
            }
            var type = (JsonItemType)bytes.buffer[pos];
            switch (type) {
                case JsonItemType.Null:
                case JsonItemType.True:
                case JsonItemType.False:
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
                case JsonItemType.JSON:
                case JsonItemType.ByteString:
                    next = pos + 1 + 4 + bytes.ReadInt32(pos + 1);      // byte count
                    return type;
                case JsonItemType.CharString:
                    next = pos + 1 + 4 + 2 * bytes.ReadInt32(pos + 1);  // char count (not byte count)
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
        
        public ReadOnlySpan<byte> ReadByteSpan(int pos) {
            var len = bytes.ReadInt32(pos + 1);
            return new ReadOnlySpan<byte>(bytes.buffer, pos + 1 + 4, len);
        }
        
        public Bytes ReadBytes(int pos) {
            var len     = bytes.ReadInt32(pos + 1);
            var start   =  pos + 1 + 4;
            return new Bytes { buffer = bytes.buffer, start = start, end = start + len };
        }
        
        public ReadOnlySpan<char> ReadCharSpan(int pos) {
            var len = bytes.ReadInt32(pos + 1);  // len = char count (not byte count)
            return bytes.GetCharSpan(pos + 1 + 4, len);
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
        
        public string AsString() {
            var sb = new StringBuilder();
            sb.Append("Count: ");
            sb.Append(count);
            sb.Append(" [");
            int pos = bytes.start;
            while (true)
            {
                var type = GetItemType(pos, out int next);
                if (type == JsonItemType.End) {
                    break;
                }
                AppendItem(sb, type, pos);
                pos = next;
            }
            if (count > 0) {
                sb.Length -= 2;
            }
            sb.Append(']');
            return sb.ToString();
        }
        
        private void AppendItem(StringBuilder sb, JsonItemType type, int pos)
        {
            switch (type) {
                case JsonItemType.Null:         sb.Append("null");              break;
                case JsonItemType.True:         sb.Append("true");              break;
                case JsonItemType.False:        sb.Append("false");             break;
                //
                case JsonItemType.Uint8:        sb.Append(ReadUint8     (pos)); break;
                case JsonItemType.Int16:        sb.Append(ReadInt16     (pos)); break;
                case JsonItemType.Int32:        sb.Append(ReadInt32     (pos)); break;
                case JsonItemType.Int64:        sb.Append(ReadInt64     (pos)); break;
                //
                case JsonItemType.Flt32:
                    var flt = ReadFlt32(pos).ToString(CultureInfo.InvariantCulture);
                    sb.Append(flt);
                    break;
                case JsonItemType.Flt64:
                    var dbl = ReadFlt64(pos).ToString(CultureInfo.InvariantCulture);
                    sb.Append(dbl);
                    break;
                //
                case JsonItemType.JSON: {
                    var value = ReadBytes(pos);
                    var str = Utf8.GetString(value.buffer, value.start, value.Len);
                    sb.Append(str);
                    break;
                }
                case JsonItemType.ByteString: {
                    var value = ReadBytes(pos);
                    var str = Utf8.GetString(value.buffer, value.start, value.Len);
                    sb.Append('\'');
                    sb.Append(str);
                    sb.Append('\'');
                    break;
                }
                case JsonItemType.CharString:
                    sb.Append('\'');
                    sb.Append(ReadCharSpan(pos));
                    sb.Append('\'');
                    break;
                case JsonItemType.DateTime:
                    var dateTime = ReadDateTime(pos).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    sb.Append(dateTime);
                    break;
                case JsonItemType.Guid:         sb.Append(ReadGuid      (pos)); break;
                default:
                    throw new InvalidOperationException($"unexpected type: {type}");
            }
            sb.Append(", ");
        }
        
    }
    
    public enum JsonItemType
    {
        Null        =  0,   // 1 byte
        //
        True        =  1,   // 1
        False       =  2,   // 1
        // --- integer
        Uint8       =  3,   // 1 + 1
        Int16       =  4,   // 1 + 2
        Int32       =  5,   // 1 + 4
        Int64       =  6,   // 1 + 8
        //
        Flt32       =  7,   // 1 + 4
        Flt64       =  8,   // 1 + 8
        //
        JSON        =  9,   // 1 + 4 + byte count (array or object, is written as is)
        ByteString  = 10,   // 1 + 4 + byte count
        CharString  = 11,   // 1 + 4 + char count (2 * char count = byte count)
        //
        DateTime    = 12,   // 1 + 8
        Guid        = 13,   // 1 + 16
        //
        End         = 14,   // 0 - Note: End is not written to JsonArray.bytes
    }
}