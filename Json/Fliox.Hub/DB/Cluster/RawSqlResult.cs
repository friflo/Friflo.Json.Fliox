// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Friflo.Json.Burst;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable InconsistentNaming
// ReSharper disable ReturnTypeCanBeEnumerable.Global
namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
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
    
    public readonly struct RawSqlRow
    {
        // --- public
                        public  readonly    int                     index;
                        public              int                     count       => rawResult.columnCount;
                        public              ReadOnlySpan<FieldType> types       => new ReadOnlySpan<FieldType>(rawResult.types);
        // --- private
        [Browse(Never)] private readonly    RawSqlResult            rawResult;

                        public override     string                  ToString()  => GetString();

        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);


        internal RawSqlRow(RawSqlResult rawResult, int index) {
            this.index      = index;
            this.rawResult  = rawResult;
        }
        
        public bool IsNull(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            return type == JsonItemType.Null;
        }
        
        public string GetString(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.Null:         return null;
                //
                case JsonItemType.ByteString:   return BytesToString(rawResult.values.ReadBytes(pos));
                case JsonItemType.CharString:   return rawResult.values.ReadCharSpan(pos).ToString();
                //
                case JsonItemType.Guid:         return rawResult.values.ReadGuid(pos).ToString();   // TODO can be remove?
                case JsonItemType.DateTime:     return rawResult.values.ReadDateTime(pos).ToString(Bytes.DateTimeFormat, CultureInfo.InvariantCulture); // TODO can be remove?
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }
        
        public string GetJSON(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.Null:         return null;
                case JsonItemType.True:         return "true";
                case JsonItemType.False:        return "false";
                //
                case JsonItemType.Uint8:        return rawResult.values.ReadUint8(pos).ToString(CultureInfo.InvariantCulture);
                case JsonItemType.Int16:        return rawResult.values.ReadInt16(pos).ToString(CultureInfo.InvariantCulture);
                case JsonItemType.Int32:        return rawResult.values.ReadInt32(pos).ToString(CultureInfo.InvariantCulture);
                case JsonItemType.Int64:        return rawResult.values.ReadInt64(pos).ToString(CultureInfo.InvariantCulture);
                //
                case JsonItemType.Flt32:        return rawResult.values.ReadFlt32(pos).ToString(CultureInfo.InvariantCulture);
                case JsonItemType.Flt64:        return rawResult.values.ReadFlt64(pos).ToString(CultureInfo.InvariantCulture);
                //
                case JsonItemType.JSON:
                case JsonItemType.ByteString:   return BytesToString(rawResult.values.ReadBytes(pos));
                case JsonItemType.CharString:   return rawResult.values.ReadCharSpan(pos).ToString();
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }
        
        public bool GetBoolean(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.True:
                case JsonItemType.False:        return rawResult.values.ReadBool(pos);  // TODO use only one case
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }
        
        public byte GetByte(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.Uint8:    return rawResult.values.ReadUint8(pos);
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }

        public short GetInt16(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.Uint8:    return rawResult.values.ReadUint8(pos);
                case JsonItemType.Int16:    return rawResult.values.ReadInt16(pos);
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }
        
        public int GetInt32(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.Uint8:    return rawResult.values.ReadUint8(pos);
                case JsonItemType.Int16:    return rawResult.values.ReadInt16(pos);
                case JsonItemType.Int32:    return rawResult.values.ReadInt32(pos);
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }
        
        public long GetInt64(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.Uint8:    return rawResult.values.ReadUint8(pos);
                case JsonItemType.Int16:    return rawResult.values.ReadInt16(pos);
                case JsonItemType.Int32:    return rawResult.values.ReadInt32(pos);
                case JsonItemType.Int64:    return rawResult.values.ReadInt64(pos);
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }
        
        public float GetFlt32(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.Flt32:    return rawResult.values.ReadFlt32(pos);
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }
        
        public double GetFlt64(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.Flt32:    return rawResult.values.ReadFlt32(pos);
                case JsonItemType.Flt64:    return rawResult.values.ReadFlt64(pos);
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }
        
        public Guid GetGuid(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.Guid:     return rawResult.values.ReadGuid(pos);
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }
        
        public DateTime GetDateTime(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.DateTime: return rawResult.values.ReadDateTime(pos);
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }

        
        private static string BytesToString(in Bytes bytes) {
            return Utf8.GetString(bytes.buffer, bytes.start, bytes.end - bytes.start);
        }
        
        private string GetString() {
            var indexes     = rawResult.GetIndexes();
            var columnCount = rawResult.columnCount; 
            var first       = index * columnCount;
            var start       = indexes[first];
            var end         = indexes[first + columnCount];
            var array       = new JsonArray(columnCount, rawResult.values, start, end);
            return array.AsString();
        }
    }
    
    public enum FieldType
    {
        Unknown     =  0,
        //
        Bool        =  1,
        //
        UInt8       =  2,
        Int16       =  3,
        Int32       =  4,
        Int64       =  5,
        //
        String      =  6,
        DateTime    =  7,
        Guid        =  8,
        //
        Float       =  9,
        Double      = 10,
        //
        JSON        = 11,
    }
}