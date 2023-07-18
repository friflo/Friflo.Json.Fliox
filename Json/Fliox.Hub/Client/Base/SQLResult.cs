// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public class SQLResult2
    {
        public string       error;
        public List<SqlRow> rows;
    }
    
    public struct SqlRow
    {
        public SqlValue[] values;
    }
    
    public struct SqlValue
    {
        public long         lng;
        public double       dbl;
        public string       str;
        public DateTime     dateTime;
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