// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Schema.Definition;

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
                var key         = cells[keyColumn.ordinal].AsKey(keyColumn.typeId);
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
            if (column.columnType == ColumnType.Array) {
                GetString(ref cell.chars, ordinal);
                return;
            }
            switch (column.typeId) {
                case StandardTypeId.Boolean:    cell.lng = reader.GetBoolean    (ordinal) ? 1 : 0;  return;
                
                case StandardTypeId.String:     
                case StandardTypeId.Enum:
                case StandardTypeId.BigInteger: GetString(ref cell.chars, ordinal);         return;
                    
                case StandardTypeId.Uint8:      cell.lng = reader.GetByte       (ordinal);  return;
                case StandardTypeId.Int16:      cell.lng = reader.GetInt16      (ordinal);  return;
                case StandardTypeId.Int32:      cell.lng = reader.GetInt32      (ordinal);  return;
                case StandardTypeId.Int64:      cell.lng = reader.GetInt64      (ordinal);  return;
                //
                case StandardTypeId.Float:      cell.dbl = reader.GetFloat      (ordinal);  return;
                case StandardTypeId.Double:     cell.dbl = reader.GetDouble     (ordinal);  return;
                //
                case StandardTypeId.DateTime:   cell.date= reader.GetDateTime   (ordinal);  return;
                case StandardTypeId.Guid:       cell.guid= reader.GetGuid       (ordinal);  return;
                default:
                    throw new InvalidOperationException($"unexpected typeId: {column.typeId}");
            }
        }
        
        private void GetString(ref Chars chars, int ordinal) {
            var len = (int)reader.GetChars(ordinal, 0, null, 0, 0);
            if (len > charBuf.Length - charPos) {
                charBuf = new char[len];
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
                        if (!HasNonNullMembers(objectMember)) {
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
            if (column.columnType == ColumnType.Array) {
                var bytes = Chars2Bytes(cell.chars);
                writer.MemberArr(key, bytes);
                return;
            }
            switch (column.typeId) {
                case StandardTypeId.Boolean:    writer.MemberBln    (key, cell.lng != 0);       break;
                //
                case StandardTypeId.String:
                case StandardTypeId.Enum:
                case StandardTypeId.BigInteger: writer.MemberStr    (key, cell.chars.AsSpan()); break;
                //
                case StandardTypeId.Uint8:
                case StandardTypeId.Int16:
                case StandardTypeId.Int32:
                case StandardTypeId.Int64:      writer.MemberLng    (key, cell.lng);            break;
                //
                case StandardTypeId.Float:
                case StandardTypeId.Double:     writer.MemberDbl    (key, cell.dbl);            break;
                //
                case StandardTypeId.Guid:       writer.MemberGuid   (key,cell.guid);            break;
                case StandardTypeId.DateTime:   writer.MemberDate   (key,cell.date);            break;
                default:
                    throw new InvalidOperationException($"unexpected typeId: {column.typeId}");
            }
        }
        
        private bool HasNonNullMembers(ObjectInfo obj) {
            foreach (var member in obj.members) {
                switch (member) {
                    case ColumnInfo column:
                        if (!cells[column.ordinal].isNull) {
                            return true;
                        }
                        break;
                    case ObjectInfo objectMember:
                        if (HasNonNullMembers(objectMember)) {
                            return true;
                        }
                        break;
                }
            }
            return false;
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
        
        internal JsonKey AsKey(StandardTypeId  typeId)
        {
            switch (typeId) {
                case StandardTypeId.String:     return new JsonKey(chars.GetString());
                case StandardTypeId.Uint8:      return new JsonKey(lng);
                case StandardTypeId.Int16:      return new JsonKey(lng);
                case StandardTypeId.Int32:      return new JsonKey(lng);
                case StandardTypeId.Int64:      return new JsonKey(lng);
                //
                case StandardTypeId.Guid:       return new JsonKey(guid);
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