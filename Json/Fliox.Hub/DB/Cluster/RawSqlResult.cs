// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable ReturnTypeCanBeEnumerable.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    /// <summary>Used to execute raw SQL commands</summary>
    public sealed class RawSql
    {
        /// <summary>An SQL statement like: <c>select * from table_name;</c></summary>
        [Required]  public string   command;
        /// <summary> If true the response contains the schema <see cref="RawSqlResult.columns"/> in the response</summary>
                    public bool?    schema;

        public RawSql() {}
        
        public RawSql(string command, bool schema = false) {
            this.command    = command;
            this.schema     = schema ? true : null;
        }
    }
    
    public sealed class RawSqlResult
    {
        // --- public
        /// <summary>number of returned rows</summary>
        [Serialize]     public  int             rowCount    { get; internal set; }
        [Serialize]     public  int             columnCount { get; internal set; }
        /// <summary>The columns returned by a raw SQL query</summary>
                        public  RawSqlColumn[]  columns;
        /// <summary>An array of all query result values. In total: <see cref="rowCount"/> * <see cref="columnCount"/> values</summary>
                        public  JsonTable       data;
                        public  RawSqlRow[]     Rows        => rows ?? GetRows();
        
        // --- private / internal
        [Browse(Never)] private int[]           indexArray;
        [Browse(Never)] private RawSqlRow[]     rows;

        public override         string          ToString()  => $"rows: {rowCount}, columns: {columnCount}";

        public RawSqlResult() { } // required for serialization
        
        public RawSqlResult(RawSqlColumn[] columns, JsonTable data, int rowCount) {
            this.columns        = columns;
            this.data           = data;
            this.columnCount    = columns.Length;
            this.rowCount       = rowCount;
            if (this.rowCount != rowCount) {
                throw new InvalidComObjectException($"invalid rowCount. expected: {rowCount}. was: {this.rowCount}");
            }
        }

        public   RawSqlRow      GetRow(int row) {
            if (row < 0 || row >= rowCount) throw new IndexOutOfRangeException(nameof(row));
            return new RawSqlRow(this, row);
        }
        
        private RawSqlRow[] GetRows() { 
            var result      = new RawSqlRow[rowCount];
            for (int row = 0; row < rowCount; row++) {
                result[row] = new RawSqlRow(this, row);
            }
            return result;
        }

        internal int[] GetIndexes() {
            if (indexArray != null) {
                return indexArray;
            }
            var indexes = new int[data.ItemCount + 1];
            int n   = 0;
            int pos = 0;
            while (true)
            {
                var type = data.GetItemType(pos, out int next);
                if (type == JsonItemType.End) {
                    break;
                }
                if (type == JsonItemType.NewRow) {
                    pos = next;
                    continue;                    
                }
                indexes[n++] = pos;
                pos = next;
            }
            indexes[n] = pos;
            return indexArray = indexes;
        }
        
        internal JsonItemType GetValue(int index, int ordinal, out int pos) {
            var valueIndex  = columnCount * index + ordinal;
            var indexes     = GetIndexes();
            pos             = indexes[valueIndex];
            return data.GetItemType(pos);
        }
    }
}
