// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    /** The column type used by an SQL database. */
    public enum FieldType
    {
        Unknown     =  0,
        //
        /** Not supported by all SQL database. SQLite, SQL Server, MySQL, MariaDB: tinyint */
        Bool        =  1,
        //
        /** Not supported by all SQL database. SQLite: integer, PostgreSQL: smallint */
        UInt8       =  2,
        /** Not supported by all SQL database. SQLite: integer */
        Int16       =  3,
        /** Not supported by all SQL database. SQLite: integer */
        Int32       =  4,
        Int64       =  5,
        //
        String      =  6,
        /** Not supported by all SQL database. SQLite: text */
        DateTime    =  7,
        /** Not supported by all SQL database. SQLite: text, MySQL: varchar(36) */
        Guid        =  8,
        //
        /** Not supported by all SQL database. SQLite: real */
        Float       =  9,
        Double      = 10,
        //
        /** Not supported by all SQL database. SQLite: text, SQL Server: nvarchar(max), MariaDB: longtext */
        JSON        = 11,
    }
    
    public class RawSqlResult
    {
        // --- public
        /// <summary>number of returned rows</summary>
        [Serialize]     public  int             rowCount    { get; internal set; }
        /// <summary>The column types of a query result</summary>
                        public  FieldType[]     types;
        /// <summary>An array of all query result values. In total: <see cref="rowCount"/> * <see cref="columnCount"/> values</summary>
                        public  JsonArray       values;
                        public  int             columnCount => types.Length;
                        public  RawSqlRow[]     Rows        => rows ?? GetRows();
        
        // --- private / internal
        [Browse(Never)] private int[]           indexArray;
        [Browse(Never)] private RawSqlRow[]     rows;

        public override         string          ToString()  => $"rows: {rowCount}, columns; {columnCount}";

        public RawSqlResult() { }
        public RawSqlResult(FieldType[] types, JsonArray values, int rowCount) {
            this.types      = types;
            this.values     = values;
            this.rowCount   = values.Count / types.Length;
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
