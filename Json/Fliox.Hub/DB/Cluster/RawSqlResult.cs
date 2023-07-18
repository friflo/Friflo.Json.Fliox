// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    public class RawSqlResult
    {
        public int              rows;
        public int              columns;
        public List<JsonKey>    values;

        public override string ToString() => GetString();
        
        private string GetString() {
            return $"rows: {rows}, columns; {columns}";
        }
    }
    
    public struct RawSqlRow
    {
        public JsonKey[] values;
        
        public override string ToString() => GetString();
        
        private string GetString() {
            if (values != null) {
                return $"values: {values.Length}";
            }
            return null;
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