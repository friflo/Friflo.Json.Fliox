// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Protocol.Models;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public sealed class SQL2Json : IDisposable
    {
        public  readonly    List<EntityValue>   result = new List<EntityValue>();
        public              Utf8JsonWriter      writer;
        public              ReadCell[]          cells       = new ReadCell[4];  // reused
        // --- private
        private             Bytes               bytesBuf    = new Bytes(8);     // reused
        private             byte[]              buffer      = new byte[16];     // reused
        private             char[]              charBuf     = new char[16];     // reused
        private             int                 charPos;
        private             ISQL2JsonMapper     mapper;
        private             TableInfo           tableInfo;
        
        public void InitMapper(ISQL2JsonMapper mapper, TableInfo tableInfo) {
            this.tableInfo  = tableInfo;
            this.mapper     = mapper;
            var columns     = tableInfo.columns;
            var colLen      = columns.Length;
            if (colLen > cells.Length) {
                cells = new ReadCell[colLen];
            }
            result.Clear();
            charPos = 0;
        }
        
        public void AddRow() {
            // --- create JSON entity
            writer.InitSerializer();
            writer.ObjectStart();
            Traverse(tableInfo.root);
            writer.ObjectEnd();
                
            var keyColumn   = tableInfo.keyColumn;
            var key         = cells[keyColumn.ordinal].AsKey(keyColumn.type);
            var value       = new JsonValue(writer.json.AsArray()); // TODO - OPTIMIZE: use MemoryBuffer to avoid array creation
            result.Add(new EntityValue(key, value));
            charPos         = 0;
            bytesBuf.start  = 0;
            bytesBuf.end    = 0;
        }
        
        public void GetString(DbDataReader reader, ref ReadCell cell, int ordinal) {
            var len = (int)reader.GetChars(ordinal, 0, null, 0, 0);
            if (len > charBuf.Length - charPos) {
                charBuf = new char[len + charBuf.Length]; // ensure buffer is only growing
                charPos = 0;
            }
            reader.GetChars(ordinal, 0, charBuf, charPos, len);
            cell.chars.start    = charPos;
            cell.chars.len      = len;
            cell.chars.buf      = charBuf;
            cell.isCharString   = true;
            charPos += len;
        }
        
        public void CopyBytes(in ReadOnlySpan<byte> bytes, ref ReadCell cell) {
            var start = bytesBuf.end;
            bytesBuf.AppendBytesSpan(bytes);
            cell.bytes.buffer   = bytesBuf.buffer;
            cell.bytes.start    = start;
            cell.bytes.end      = bytesBuf.end;
            cell.isCharString   = false;
        }
        
        private void Traverse(ObjectInfo obj)
        {
            foreach (var member in obj.members) {
                switch (member) {
                    case ColumnInfo column:
                        mapper.WriteColumn(this, column);
                        break;
                    case ObjectInfo objectMember:
                        if (cells[objectMember.ordinal].isNull) {
                            // writer.MemberNul(objectMember.nameBytes); // omit writing member with value null
                            continue;
                        }
                        writer.MemberObjectStart(objectMember.nameBytes);
                        Traverse(objectMember);
                        writer.ObjectEnd();
                        break;
                }
            }
        }
        
        public Bytes Chars2Bytes (in Chars value)
        {
            var max = Encoding.UTF8.GetMaxByteCount(value.len);
            if (max > buffer.Length) buffer = new byte[max];
            int len = Encoding.UTF8.GetBytes(value.buf, value.start, value.len, buffer, 0);
            return new Bytes{ buffer = buffer, start = 0, end = len };
        }
        
        public Bytes String2Bytes (string value)
        {
            var max = Encoding.UTF8.GetMaxByteCount(value.Length);
            if (max > buffer.Length) buffer = new byte[max];
            int len = Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, 0);
            return new Bytes{ buffer = buffer, start = 0, end = len };
        }

        public void Dispose() {
            writer.Dispose();
        }
    }
    
    public struct ReadCell
    {
        public      bool        isNull;
        internal    Chars       chars;
        public      Bytes       bytes;
        internal    bool        isCharString;
        public      string      str;
        public      long        lng;
        public      double      dbl;
        public      Guid        guid;
        public      DateTime    date;
        
        public      ReadOnlySpan<char> CharsSpan() => chars.AsSpan();
        public      ReadOnlySpan<byte> BytesSpan() => bytes.AsSpan();
        
        internal JsonKey AsKey(ColumnType typeId)
        {
            switch (typeId) {
                case ColumnType.String:     return isCharString
                                                ? new JsonKey(chars.GetString())  // TODO - OPTIMIZE
                                                : new JsonKey(bytes.AsSpan(), default);
                case ColumnType.Uint8:      return new JsonKey(lng);
                case ColumnType.Int16:      return new JsonKey(lng);
                case ColumnType.Int32:      return new JsonKey(lng);
                case ColumnType.Int64:      return new JsonKey(lng);
                //
                case ColumnType.Guid:       return new JsonKey(guid);
                default:
                    throw new NotSupportedException($"primary key type not supported: {typeId}");
            }
        }
    }
    
    public struct Chars {
        internal    char[]  buf;
        internal    int     start;
        internal    int     len;

        public override string ToString() => GetString();

        internal ReadOnlySpan<char> AsSpan() {
            return new ReadOnlySpan<char>(buf, start, len);
        }
        
        internal string GetString() {
            return AsSpan().ToString();
        }
    }
}