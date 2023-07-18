// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    public class RawSqlResult
    {
        public  int             rowCount;
        public  int             columnCount;
        public  JsonKey[]       values;
        
        public  RawSqlRow[]     Rows        => GetRows();
        public  override string ToString()  => GetString();

        public RawSqlRow GetRow(int row) {
            if (row < 0 || row >= rowCount) throw new IndexOutOfRangeException(nameof(row));
            return new RawSqlRow(values, row, columnCount);
        }

        public JsonKey GetValue(int row, int column) {
            if (row    < 0  || row    >= rowCount)      throw new IndexOutOfRangeException(nameof(row));
            if (column < 0  || column >= columnCount)   throw new IndexOutOfRangeException(nameof(column));
            return values[row * columnCount + column];
        }

        private string GetString() {
            return $"rows: {rowCount}, columns; {columnCount}";
        }
        
        private RawSqlRow[] GetRows() { 
            var result = new RawSqlRow[rowCount];
            for (int row = 0; row < rowCount; row++) {
                result[row] = new RawSqlRow(values, row, columnCount);
            }
            return result;
        }
    }
    
    public readonly struct RawSqlRow
    {
        public  readonly    int         index;
        public  readonly    int         count;
        [Browse(Never)]
        private readonly    JsonKey[]   values;

        public  override    string      ToString()          => $"row: {index}";
        public ReadOnlySpan<JsonKey>    Values              => values.AsSpan().Slice(index * count, count);
        public JsonKey                  this[int column]    => values[index * count + column];

        internal RawSqlRow(JsonKey[] values, int index, int count) {
            this.values = values;
            this.index  = index;
            this.count  = count;
        }
    }
    
   
    // TODO rename?
    public enum FieldType
    {
        None,
        UInt8,
        Int16,
        Int32,
        Int64,
        String,
        DateTime,
        Double,
        Float,
    }
}