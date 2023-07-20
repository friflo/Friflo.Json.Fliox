// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Text;
using Friflo.Json.Burst;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
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
            var type = rawResult.GetValue(index, ordinal, out _);
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
                //
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
            var type = rawResult.GetValue(index, ordinal, out _);
            switch (type) {
                case JsonItemType.True:         return true; 
                case JsonItemType.False:        return false;
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }
        
        public byte GetByte(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.Uint8:        return rawResult.values.ReadUint8(pos);
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }

        public short GetInt16(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.Uint8:        return rawResult.values.ReadUint8(pos);
                case JsonItemType.Int16:        return rawResult.values.ReadInt16(pos);
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }
        
        public int GetInt32(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.Uint8:        return rawResult.values.ReadUint8(pos);
                case JsonItemType.Int16:        return rawResult.values.ReadInt16(pos);
                case JsonItemType.Int32:        return rawResult.values.ReadInt32(pos);
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }
        
        public long GetInt64(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.Uint8:        return rawResult.values.ReadUint8(pos);
                case JsonItemType.Int16:        return rawResult.values.ReadInt16(pos);
                case JsonItemType.Int32:        return rawResult.values.ReadInt32(pos);
                case JsonItemType.Int64:        return rawResult.values.ReadInt64(pos);
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }
        
        public float GetFlt32(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.Flt32:        return rawResult.values.ReadFlt32(pos);
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }
        
        public double GetFlt64(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.Flt32:        return rawResult.values.ReadFlt32(pos);
                case JsonItemType.Flt64:        return rawResult.values.ReadFlt64(pos);
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }
        
        public Guid GetGuid(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.Guid:         return rawResult.values.ReadGuid(pos);
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }
        
        public DateTime GetDateTime(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.DateTime:     return rawResult.values.ReadDateTime(pos);
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
}