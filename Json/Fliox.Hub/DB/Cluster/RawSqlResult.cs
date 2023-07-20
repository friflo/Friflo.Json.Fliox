// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    public class RawSqlResult
    {
        // --- public
        /// <summary>number of returned rows</summary>
        [Serialize]     public  int             rowCount    { get; internal set; }
        /// <summary>The columns returned by a raw SQL query</summary>
                        public  RawSqlColumn[]  columns;
        /// <summary>An array of all query result values. In total: <see cref="rowCount"/> * <see cref="columnCount"/> values</summary>
                        public  JsonArray       values;
                        public  int             columnCount => columns.Length;
                        public  RawSqlRow[]     Rows        => rows ?? GetRows();
        
        // --- private / internal
        [Browse(Never)] private int[]           indexArray;
        [Browse(Never)] private RawSqlRow[]     rows;

        public override         string          ToString()  => $"rows: {rowCount}, columns; {columnCount}";

        public RawSqlResult() { } // required for serialization
        
        public RawSqlResult(RawSqlColumn[] columns, JsonArray values, int rowCount) {
            this.columns    = columns;
            this.values     = values;
            this.rowCount   = values.Count / columns.Length;
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
            var indexes = new int[values.Count + 1];
            int n   = 0;
            int pos = 0;
            while (true)
            {
                var type = values.GetItemType(pos, out int next);
                if (type == JsonItemType.End) {
                    break;
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
            return values.GetItemType(pos);
        }
    }
}
