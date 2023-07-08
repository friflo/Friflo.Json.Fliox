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
        private     ReadCell[]      cells;
        private     byte[]          buffer = new byte[16];
        
        public async Task<List<EntityValue>> ReadEntitiesAsync(DbDataReader reader, TableInfo tableInfo)
        {
            var columns = tableInfo.columns;
            cells       = new ReadCell[columns.Length];
            this.reader = reader;
            var result  = new List<EntityValue>();
            while (await reader.ReadAsync().ConfigureAwait(false)) {
                writer.InitSerializer();
                foreach (var column in columns) {
                    ReadCell(column, ref cells[column.ordinal]);
                }
                Traverse(tableInfo.root);
                var keyColumn   = tableInfo.keyColumn;
                var key         = cells[keyColumn.ordinal].AsKey(keyColumn.typeId);
                var value       = new JsonValue(writer.json.AsArray()); // TODO - use MemoryBuffer to avoid array creation
                result.Add(new EntityValue(key, value));
            }
            this.reader = null;
            cells       = null;
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
                cell.str = reader.GetString(ordinal);
                return;
            }
            switch (column.typeId) {
                case StandardTypeId.Boolean:    cell.lng = reader.GetBoolean    (ordinal) ? 1 : 0;  return;
                case StandardTypeId.String:     cell.str = reader.GetString     (ordinal);  return;
                    
                case StandardTypeId.Uint8:      cell.lng = reader.GetByte       (ordinal);  return;
                case StandardTypeId.Int16:      cell.lng = reader.GetInt16      (ordinal);  return;
                case StandardTypeId.Int32:      cell.lng = reader.GetInt32      (ordinal);  return;
                case StandardTypeId.Int64:      cell.lng = reader.GetInt64      (ordinal);  return;
                //
                case StandardTypeId.Float:      cell.dbl = reader.GetFloat      (ordinal);  return;
                case StandardTypeId.Double:     cell.dbl = reader.GetDouble     (ordinal);  return;
                //
                case StandardTypeId.BigInteger: cell.str = reader.GetString     (ordinal);  return;
                case StandardTypeId.DateTime:
                    var dateTime                         = reader.GetDateTime   (ordinal);
                    cell.str = dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                    return;
                case StandardTypeId.Guid:           
                    var guid                             = reader.GetGuid       (ordinal);
                    cell.str = guid.ToString();
                    return;
            }
        }
        
        private void Traverse(ObjectInfo obj) {
            writer.ObjectStart();
            foreach (var column in obj.columns) {
                ref var cell    = ref cells[column.ordinal];
                var key         = column.nameBytes;
                if (cell.isNull) {
                    writer.MemberNul(key); // could omit writing a member with value null
                    continue;
                }
                cell.isNull = true;
                if (column.columnType == ColumnType.Array) {
                    var bytes = String2Bytes(cell.str);
                    writer.MemberArr(key, bytes);
                    continue;
                }
                switch (column.typeId) {
                    case StandardTypeId.Boolean:    writer.MemberBln(key, cell.lng != 0);   break;
                    case StandardTypeId.String:     writer.MemberStr(key, cell.str);        break;
                    //
                    case StandardTypeId.Uint8:
                    case StandardTypeId.Int16:
                    case StandardTypeId.Int32:
                    case StandardTypeId.Int64:      writer.MemberLng(key, cell.lng);        break;
                    //
                    case StandardTypeId.Float:
                    case StandardTypeId.Double:
                                                    writer.MemberDbl(key, cell.dbl);        break;
                    case StandardTypeId.BigInteger:
                    case StandardTypeId.Guid:
                                                    writer.MemberStr(key, cell.str);        break;
                }
            }
            writer.ObjectEnd();            
        }
        
        private Bytes String2Bytes (string value) {
            var max = Encoding.UTF8.GetMaxByteCount(value.Length);
            if (max > buffer.Length) buffer = new byte[max];
            int len = Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, 0);
            return new Bytes{ buffer = buffer, start = 0, end = len };
        }

        public void Dispose() {
            writer.Dispose();
        }
    }
    
    internal struct ReadCell
    {
        internal    bool    isNull;
        internal    string  str;
        internal    long    lng;
        internal    double  dbl;
        
        internal JsonKey AsKey(StandardTypeId  typeId)
        {
            switch (typeId) {
                case StandardTypeId.String:     return new JsonKey(str);
                    
                case StandardTypeId.Uint8:      return new JsonKey(lng);
                case StandardTypeId.Int16:      return new JsonKey(lng);
                case StandardTypeId.Int32:      return new JsonKey(lng);
                case StandardTypeId.Int64:      return new JsonKey(lng);
                //
                case StandardTypeId.BigInteger: return new JsonKey(str);
                case StandardTypeId.DateTime:   return new JsonKey(str);
                case StandardTypeId.Guid:       return new JsonKey(str);
                default:
                    throw new NotSupportedException($"primary key type not supported: {typeId}");
            }
        }
    }
}