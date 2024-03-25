// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Mapper.Map;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox
{
    public sealed class JsonTable
    {
        public      int             RowCount    => rowCount + (RowItemCount > 0 ? 1 : 0);
        public      int             ColumnCount => GetColumnCount();
        public      int             ItemCount   => itemCount;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] // output can be very long
        public      string          TableString => GetTableString();

        // --- private
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] // output can be very long
        private     Bytes           bytes;
        private     int             rowCount;
        private     int             itemCount;
        private     int             columnCount;
        private     int             startRowCount;
        //
        internal    int[]           indexes;
        
        private     int             RowItemCount        => itemCount - startRowCount;
        public      JsonTableRow[]  CreateTableRows()   => JsonTableRow.CreateTableRows(this);
        
        public override string  ToString()  => AppendString(new StringBuilder()).ToString();
        
        public JsonTable() {
            bytes = new Bytes(32);
        }
        
        private int GetColumnCount() {
            var rowItemCount = RowItemCount;
            switch (RowCount) {
                case 0: return rowItemCount;
                case 1: return rowItemCount == 0 ? columnCount : rowItemCount;
            }
            return rowItemCount == 0 ||
                   rowItemCount == columnCount ? columnCount : -1;
        }
        
        public JsonTable(int itemCount, JsonTable data, int start, int end) {
            this.itemCount  = itemCount;           
            bytes.buffer    = data.bytes.buffer;
            bytes.start     = start;
            bytes.end       = end;
        }
        
        public void Init() {
            rowCount        = 0;
            itemCount       = 0;
            columnCount     = 0;
            startRowCount   = 0;
            bytes.start     = 0;
            bytes.end       = 0;
        }
        
        /// <summary>using instead of <see cref="DateTime.ToBinary"/> which degrade performance by x100</summary>
        private static long DateTime2Lng(DateTime dateTime) {
            return dateTime.Ticks | (long)dateTime.Kind << DateTimeKindShift;
        }
        
        /// <summary>using instead of <see cref="DateTime.FromBinary"/> which degrade performance by x100</summary>
        private static DateTime Lng2DateTime(long lng) {
            return new DateTime(lng & DateTimeMaskTicks, (DateTimeKind)((ulong)lng >> DateTimeKindShift));
        }
        
        private const int   DateTimeKindShift = 62;
        private const long  DateTimeMaskTicks = 0x3FFFFFFFFFFFFFFF;
        
        // -------------------------------------------- write --------------------------------------------
        public void WriteNull() {
            bytes.EnsureCapacity(1);
            bytes.buffer[bytes.end++] = (byte)JsonItemType.Null;
            itemCount++;
        }
        
        public void WriteBoolean(bool value) {
            bytes.EnsureCapacity(1);
            bytes.buffer[bytes.end++] = (byte)(value ? JsonItemType.True : JsonItemType.False);
            itemCount++;
        }

        public void WriteByte(byte value) {
            bytes.EnsureCapacity(2);
            int start = bytes.end;
            bytes.buffer[start]     = (byte)JsonItemType.Uint8;
            bytes.buffer[start + 1] = value;
            bytes.end = start + 2;
            itemCount++;
        }

        public void WriteInt16(short value) {
            if (value > byte.MaxValue || value < byte.MinValue) {
                bytes.EnsureCapacity(3);
                var start = bytes.end;
                bytes.buffer[start    ] = (byte)JsonItemType.Int16;
                bytes.buffer[start + 1] = (byte)(value >> 8);
                bytes.buffer[start + 2] = (byte)(value & 0xff);
                bytes.end = start + 3;
                itemCount++;
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
                itemCount++;
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
                itemCount++;
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
            itemCount++;
        }
        
        public void WriteFlt64(double value) {
            bytes.EnsureCapacity(9);
            var start = bytes.end;
            bytes.buffer[start] = (byte)JsonItemType.Flt64;
            bytes.WriteFlt64(start + 1, value);
            bytes.end = start + 9;
            itemCount++;
        }
        
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
        
        /// <summary>
        /// <b>Important!</b> <br/>
        /// Passed value MUST be valid JSON. Otherwise invalid JSON will be generated downstream.
        /// </summary>
        public void WriteJSON(ReadOnlySpan<byte> value) {
            itemCount++;
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
            itemCount++;
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
            itemCount++;
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
            itemCount++;
            var len     = value.Length;
            int start   = bytes.end;
            bytes.EnsureCapacity(1 + 4 + len);
            bytes.buffer[start] = (byte)JsonItemType.ByteString;
            bytes.WriteInt32(start + 1, len);
            bytes.end = start + 1 + 4;
            bytes.AppendBytesSpan(value);
        }
        
        public void WriteDateTime(in DateTime value) {
            itemCount++;
            bytes.EnsureCapacity(9);
            int start = bytes.end;
            bytes.buffer[start] = (byte)JsonItemType.DateTime;
            var lng = DateTime2Lng(value);
            bytes.WriteInt64(start + 1, lng);
            bytes.end = start + 9;
        }
        
        public void WriteGuid(in Guid value) {
            itemCount++;
            bytes.EnsureCapacity(17);
            int start = bytes.end;
            bytes.buffer[start] = (byte)JsonItemType.Guid;
            GuidUtils.GuidToLongLong(value, out long value1, out long value2);
            bytes.WriteLongLong(start + 1, value1, value2);
            bytes.end = start + 17;
        }
        
        public void WriteNewRow() {
            bytes.EnsureCapacity(1);
            bytes.buffer[bytes.end++] = (byte)JsonItemType.NewRow;
            var rowItems = RowItemCount;
            if (rowCount == 0) {
                columnCount = rowItems;
            } else {
                if (rowItems != columnCount) {
                    columnCount = -1;
                }
            }
            rowCount++;
            startRowCount = itemCount;
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
                case JsonItemType.NewRow:
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
            return Lng2DateTime(lng);
        }
        
        public Guid ReadGuid(int pos) {
            var lng1 = bytes.ReadInt64(pos + 1);
            var lng2 = bytes.ReadInt64(pos + 9);
            return GuidUtils.LongLongToGuid(lng1, lng2);
        }
        
        private StringBuilder AppendString(StringBuilder sb) {
            var rows = RowCount;
            sb.Append("rows: ");
            sb.Append(rows);
            var columns = ColumnCount;
            if (columns != -1) {
                sb.Append(", columns: ");
                sb.Append(columns);
            }
            return sb;
        }
        
        private string GetTableString() {
            var sb = new StringBuilder();
            AppendString(sb);
            sb.Append("\n[");
            AppendRows(sb, bytes.start, bytes.end);
            sb.Append(']');
            return sb.ToString();
        }
        
        public StringBuilder AppendRowItems(StringBuilder sb) {
            sb.Append("Count: ");
            sb.Append(ItemCount);
            sb.Append(" [");
            AppendRows(sb, bytes.start, bytes.end);
            sb.Append(']');
            return sb;
        }
        
        internal void AppendRows(StringBuilder sb, int start, int end) {
            int pos         = start;
            var firstItem   = true;
            while (true)
            {
                if (pos >= end) {
                    if (!firstItem) {
                        sb.Length -= 2;
                    }
                    return;
                }
                var itemType = GetItemType(pos, out int next);
                switch (itemType) {
                    case JsonItemType.End:
                        throw new InvalidOperationException("unexpected access to JsonItemType.End");
                    case JsonItemType.NewRow:
                        if (!firstItem) {
                            sb.Length -= 2;
                        }
                        pos     = next;
                        itemType    = GetItemType(pos, out next);
                        if (itemType == JsonItemType.End) {
                            return;
                        }
                        sb.Append("],\n[");
                        firstItem = true;
                        continue;
                    default:
                        AppendItem(sb, itemType, pos);
                        firstItem = false;
                        break;
                }
                pos = next;
            }
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
                case JsonItemType.Guid:
                    sb.Append(ReadGuid(pos));
                    break;
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
        NewRow      = 14,   // 1
        End         = 15,   // 0 - Note: End is not written to JsonTable.bytes
    }
}