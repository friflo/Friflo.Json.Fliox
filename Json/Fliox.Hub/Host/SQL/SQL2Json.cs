// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Protocol.Models;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public sealed class SQL2Json : IDisposable
    {
        public      Utf8JsonWriter  writer;
        public      DbDataReader    reader;
        public      ReadCell[]      cells       = new ReadCell[4];  // reused
        private     byte[]          buffer      = new byte[16];     // reused
        private     char[]          charBuf     = new char[16];     // reused
        private     int             charPos;
        private     ISQL2JsonMapper mapper;
        
        public async Task<List<EntityValue>> ReadEntitiesAsync(DbDataReader reader, TableInfo tableInfo)
        {
            mapper      = tableInfo.mapper;
            var columns = tableInfo.columns;
            var colLen  = columns.Length;
            if (colLen > cells.Length) {
                cells = new ReadCell[colLen];    
            }
            this.reader = reader;
            var result  = new List<EntityValue>();
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                // --- read table columns
                charPos = 0;
                foreach (var column in columns) {
                    mapper.ReadCell(this, column, ref cells[column.ordinal]);
                }
                // --- create JSON entity
                writer.InitSerializer();
                writer.ObjectStart();
                Traverse(tableInfo.root);
                writer.ObjectEnd();
                
                var keyColumn   = tableInfo.keyColumn;
                var key         = cells[keyColumn.ordinal].AsKey(keyColumn.type);
                var value       = new JsonValue(writer.json.AsArray()); // TODO - use MemoryBuffer to avoid array creation
                result.Add(new EntityValue(key, value));
            }
            this.reader = null;
            return result;
        }
        
        public void GetString(ref Chars chars, int ordinal) {
            var len = (int)reader.GetChars(ordinal, 0, null, 0, 0);
            if (len > charBuf.Length - charPos) {
                charBuf = new char[len + charBuf.Length]; // ensure buffer is only growing
                charPos = 0;
            }
            reader.GetChars(ordinal, 0, charBuf, charPos, len);
            chars.start = charPos;
            chars.len  = len;
            chars.buf  = charBuf;
            charPos += len;
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
                            writer.MemberNul(objectMember.nameBytes);
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
        public      Chars       chars;
        public      string      str;
        public      long        lng;
        public      double      dbl;
        public      Guid        guid;
        public      DateTime    date;
        
        internal JsonKey AsKey(ColumnType  typeId)
        {
            switch (typeId) {
                case ColumnType.String:     return new JsonKey(chars.GetString());
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

        public ReadOnlySpan<char> AsSpan() {
            return new ReadOnlySpan<char>(buf, start, len);
        }
        
        internal string GetString() {
            return AsSpan().ToString();
        }
    }
}