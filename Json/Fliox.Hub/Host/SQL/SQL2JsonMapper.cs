// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    /// <summary>
    /// <b>Important</b> implementations of <see cref="ISQL2JsonMapper"/> must not have any state.<br/>
    /// A singleton is passed to <see cref="TableInfo"/> constructor.  
    /// </summary>
    public interface ISQL2JsonMapper
    {
        void ReadCell   (SQL2Json sql2Json, ColumnInfo column, ref ReadCell cell);
        void WriteColumn(SQL2Json sql2Json, ColumnInfo column);
    }
    
    public class SQL2JsonMapper : ISQL2JsonMapper
    {
        public static readonly SQL2JsonMapper Instance = new SQL2JsonMapper();
    
        public void ReadCell(SQL2Json sql2Json, ColumnInfo column, ref ReadCell cell) {
            var ordinal = column.ordinal;
            var reader = sql2Json.reader;
            cell.isNull = reader.IsDBNull(ordinal);
            if (cell.isNull) {
                return;
            }
            switch (column.type) {
                case ColumnType.Boolean:    cell.lng = reader.GetByte       (ordinal);  return;
                //
                case ColumnType.String:     
                case ColumnType.Enum:
                case ColumnType.BigInteger: sql2Json.GetString(ref cell.chars,        ordinal);  return;
                //
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
                case ColumnType.Array:      sql2Json.GetString(ref cell.chars,        ordinal);  return;
                case ColumnType.Object:     cell.lng = reader.GetByte       (ordinal);  return; // used as boolean: != 0 => object is not null
                default:
                    throw new InvalidOperationException($"unexpected type: {column.type}");
            }
        }
        
        public void WriteColumn(SQL2Json sql2Json, ColumnInfo column)
        {
            ref var cell    = ref sql2Json.cells[column.ordinal];
            ref var writer  = ref sql2Json.writer;
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
                case ColumnType.Array:      writer.MemberArr(key, sql2Json.Chars2Bytes(cell.chars)); break;
                default:
                    throw new InvalidOperationException($"unexpected type: {column.type}");
            }
        }
    }
}