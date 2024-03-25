// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;

// ReSharper disable ConvertSwitchStatementToSwitchExpression
// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox
{
    public readonly struct JsonTableRow
    {
        private readonly   JsonTable   table;
        private readonly   int         index;
        private readonly   int         itemCount;
        
        private JsonTableRow (JsonTable table, int index, int itemCount) {
            this.table      = table;
            this.index      = index;
            this.itemCount  = itemCount;
        }
        
        public static JsonTableRow[] CreateTableRows(JsonTable table)
        {
            var rowLength       = table.RowCount;
            var rows            = new JsonTableRow[rowLength];
            var indexArray      = new int[table.ItemCount * rowLength + 1];
            int n               = 0;
            int pos             = 0;
            int rowIndex        = 0;
            int start           = 0;
            int rowItemCount    = 0;
            while (true) {
                var type = table.GetItemType(pos, out int next);
                if (type == JsonItemType.End) {
                    break;
                }
                indexArray[n] = pos;
                if (type == JsonItemType.NewRow) {
                    rows[rowIndex++] = new JsonTableRow(table, start, rowItemCount);
                    start           = n + 1; // set start index to item after JsonItemType.NewRow
                    rowItemCount    = 0;
                } else {
                    rowItemCount++;
                }
                pos = next;
                n++;
            }
            // trailing NewRow's are omitted
            if (rowIndex < rows.Length) {
                rows[rowIndex]  = new JsonTableRow(table, start, rowItemCount);
            }
            indexArray[n]   = pos;
            table.indexes   = indexArray;
            return rows;
        } 

        public override string ToString() {
            var sb      = new StringBuilder();
            var start   = table.indexes[index];
            var end     = table.indexes[index + itemCount];
            table.AppendRows(sb, start, end);
            return sb.ToString();
        }
        
        public JsonItemType GetType(int ordinal) {
            if (ordinal < 0 || ordinal >= itemCount) throw new ArgumentOutOfRangeException(nameof(ordinal));
            var pos = table.indexes[index + ordinal];
            return table.GetItemType(pos);
        }
        
        private JsonItemType GetType(int ordinal, out int pos) {
            if (ordinal < 0 || ordinal >= itemCount) throw new ArgumentOutOfRangeException(nameof(ordinal));
            pos = table.indexes[index + ordinal];
            return table.GetItemType(pos);
        }

        // ----------------------------------- getter methods -----------------------------------
        public bool IsNull(int ordinal) {
            if (ordinal < 0 || ordinal >= itemCount) throw new ArgumentOutOfRangeException(nameof(ordinal));
            var pos = table.indexes[index + ordinal];
            return table.GetItemType(pos) == JsonItemType.Null;
        }
        
        public bool GetBool (int ordinal) {
            var type = GetType(ordinal, out int pos);
            switch (type) {
                case JsonItemType.True:
                case JsonItemType.False:        return table.ReadBool(pos);
            }
            throw new InvalidCastException($"cannot return value of type: {type} as bool");
        }
        
        public byte GetByte (int ordinal) {
            var type = GetType(ordinal, out int pos);
            switch (type) {
                case JsonItemType.Uint8:        return table.ReadUint8(pos);
            }
            throw new InvalidCastException($"cannot return value of type: {type} as byte");
        }

        public short GetInt16 (int ordinal) {
            var type = GetType(ordinal, out int pos);
            switch (type) {
                case JsonItemType.Uint8:        return table.ReadUint8(pos);
                case JsonItemType.Int16:        return table.ReadInt16(pos);
            }
            throw new InvalidCastException($"cannot return value of type: {type} as short");
        }
        
        public int GetInt32 (int ordinal) {
            var type = GetType(ordinal, out int pos);
            switch (type) {
                case JsonItemType.Uint8:        return table.ReadUint8(pos);
                case JsonItemType.Int16:        return table.ReadInt16(pos);
                case JsonItemType.Int32:        return table.ReadInt32(pos);
            }
            throw new InvalidCastException($"cannot return value of type: {type} as int");
        }
        
        public long GetInt64 (int ordinal) {
            var type = GetType(ordinal, out int pos);
            switch (type) {
                case JsonItemType.Uint8:        return table.ReadUint8(pos);
                case JsonItemType.Int16:        return table.ReadInt16(pos);
                case JsonItemType.Int32:        return table.ReadInt32(pos);
                case JsonItemType.Int64:        return table.ReadInt64(pos);
            }
            throw new InvalidCastException($"cannot return value of type: {type} as long");
        }
        
        public float GetFlt32 (int ordinal) {
            var type = GetType(ordinal, out int pos);
            switch (type) {
                case JsonItemType.Flt32:        return table.ReadFlt32(pos);
            }
            throw new InvalidCastException($"cannot return value of type: {type} as float");
        }
        
        public double GetFlt64 (int ordinal) {
            var type = GetType(ordinal, out int pos);
            switch (type) {
                case JsonItemType.Flt32:        return table.ReadFlt32(pos);
                case JsonItemType.Flt64:        return table.ReadFlt64(pos);
            }
            throw new InvalidCastException($"cannot return value of type: {type} as float");
        }

        public ReadOnlySpan<char> GetCharSpan (int ordinal) {
            var type = GetType(ordinal, out int pos);
            switch (type) {
                case JsonItemType.CharString:   return table.ReadCharSpan(pos);
            }
            throw new InvalidCastException($"cannot return value of type: {type} as ReadOnlySpan<char>");
        }
        
        public ReadOnlySpan<byte> GetByteSpan (int ordinal) {
            var type = GetType(ordinal, out int pos);
            switch (type) {
                case JsonItemType.ByteString:   return table.ReadByteSpan(pos);
            }
            throw new InvalidCastException($"cannot return value of type: {type} as ReadOnlySpan<byte>");
        }
        
        public string GetString (int ordinal) {
            var type = GetType(ordinal, out int pos);
            switch (type) {
                case JsonItemType.CharString:   return table.ReadCharSpan(pos).ToString();
                case JsonItemType.ByteString:   return table.ReadBytes(pos).ToString();
            }
            throw new InvalidCastException($"cannot return value of type: {type} as string");
        }
        
        public DateTime GetDateTime (int ordinal) {
            var type = GetType(ordinal, out int pos);
            switch (type) {
                case JsonItemType.DateTime:     return table.ReadDateTime(pos);
            }
            throw new InvalidCastException($"cannot return value of type: {type} as DateTime");
        }
        
        public Guid GetGuid (int ordinal) {
            var type = GetType(ordinal, out int pos);
            switch (type) {
                case JsonItemType.Guid:         return table.ReadGuid(pos);
            }
            throw new InvalidCastException($"cannot return value of type: {type} as Guid");
        }
    }
}