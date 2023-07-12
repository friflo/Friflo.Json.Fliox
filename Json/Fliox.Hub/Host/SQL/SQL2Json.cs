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
        private     Utf8JsonWriter  writer;
        private     DbDataReader    reader;
        private     ReadCell[]      cells       = new ReadCell[4];  // reused
        private     byte[]          buffer      = new byte[16];     // reused
        private     char[]          charBuf     = new char[16];     // reused
        private     int             charPos;
        
        public async Task<List<EntityValue>> ReadEntitiesAsync(DbDataReader reader, TableInfo tableInfo)
        {
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
                    ReadCell(column, ref cells[column.ordinal]);
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
        
        private void ReadCell(ColumnInfo column, ref ReadCell cell)
        {
            var ordinal = column.ordinal;
            cell.isNull = reader.IsDBNull(ordinal);
            if (cell.isNull) {
                return;
            }
            switch (column.type) {
                case ColumnType.Boolean:    cell.lng = reader.GetBoolean    (ordinal) ? 1 : 0;  return;
                
                case ColumnType.String:     
                case ColumnType.Enum:
                case ColumnType.BigInteger: GetString(ref cell.chars, ordinal);         return;
                    
                case ColumnType.Uint8:      cell.lng = reader.GetByte       (ordinal);  return;
                case ColumnType.Int16:      cell.lng = reader.GetInt16      (ordinal);  return;
                case ColumnType.Int32:      cell.lng = reader.GetInt32      (ordinal);  return;
                case ColumnType.Int64:      cell.lng = reader.GetInt64      (ordinal);  return;
                //
                case ColumnType.Float:      cell.dbl = reader.GetFloat      (ordinal);  return;
                case ColumnType.Double:     cell.dbl = reader.GetDouble     (ordinal);  return;
                //
                case ColumnType.DateTime:   cell.date= reader.GetDateTime   (ordinal);  return;
                case ColumnType.Guid:       cell.guid= reader.GetGuid       (ordinal);  return;
                //
                case ColumnType.Array:      GetString(ref cell.chars, ordinal);         return;
                case ColumnType.Object:     cell.lng = reader.GetByte       (ordinal);  return; // used as boolean: != 0 => object is not null
                default:
                    throw new InvalidOperationException($"unexpected type: {column.type}");
            }
        }
        
        private void GetString(ref Chars chars, int ordinal) {
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
                        WriteColumn(column);
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
        
        private void WriteColumn(ColumnInfo column)
        {
            ref var cell    = ref cells[column.ordinal];
            var key         = column.nameBytes;
            if (cell.isNull) {
                writer.MemberNul(key); // could omit writing a member with value null
                return;
            }
            cell.isNull = true;
            switch (column.type) {
                case ColumnType.Boolean:    writer.MemberBln    (key, cell.lng != 0);       break;
                //
                case ColumnType.String:
                case ColumnType.Enum:
                case ColumnType.BigInteger: writer.MemberStr    (key, cell.chars.AsSpan()); break;
                //
                case ColumnType.Uint8:
                case ColumnType.Int16:
                case ColumnType.Int32:
                case ColumnType.Int64:      writer.MemberLng    (key, cell.lng);            break;
                //
                case ColumnType.Float:
                case ColumnType.Double:     writer.MemberDbl    (key, cell.dbl);            break;
                //
                case ColumnType.Guid:       writer.MemberGuid   (key, cell.guid);           break;
                case ColumnType.DateTime:   writer.MemberDate   (key, cell.date);           break;
                case ColumnType.Array:      writer.MemberArr(key, Chars2Bytes(cell.chars)); break;
                default:
                    throw new InvalidOperationException($"unexpected type: {column.type}");
            }
        }
        
        private Bytes Chars2Bytes (in Chars value)
        {
            var max = Encoding.UTF8.GetMaxByteCount(value.len);
            if (max > buffer.Length) buffer = new byte[max];
            int len = Encoding.UTF8.GetBytes(value.buf, value.start, value.len, buffer, 0);
            return new Bytes{ buffer = buffer, start = 0, end = len };
        }

        public void Dispose() {
            writer.Dispose();
        }
    }
    
    internal struct ReadCell
    {
        internal    bool        isNull;
        internal    Chars       chars;
        internal    long        lng;
        internal    double      dbl;
        internal    Guid        guid;
        internal    DateTime    date;
        
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
    
    internal struct Chars {
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