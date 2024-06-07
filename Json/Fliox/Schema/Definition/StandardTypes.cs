// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Friflo.Json.Fliox.Schema.Definition
{
    /// <summary>
    /// Contain all standard types used by a <see cref="TypeSchema"/>.
    /// Unused standard types are null.
    /// </summary>
    public abstract class StandardTypes
    {
        public abstract     TypeDef     Boolean     { get; }
        public abstract     TypeDef     String      { get; }
        
        public abstract     TypeDef     Uint8       { get; }
        public abstract     TypeDef     Int16       { get; }
        public abstract     TypeDef     Int32       { get; }
        public abstract     TypeDef     Int64       { get; }
        // NON_CLS
        public abstract     TypeDef     Int8        { get; }
        public abstract     TypeDef     UInt16      { get; }
        public abstract     TypeDef     UInt32      { get; }
        public abstract     TypeDef     UInt64      { get; }
        
        public abstract     TypeDef     Float       { get; }
        public abstract     TypeDef     Double      { get; }
        
        public abstract     TypeDef     BigInteger  { get; }
        public abstract     TypeDef     DateTime    { get; }
        public abstract     TypeDef     Guid        { get; }
        
        public abstract     TypeDef     JsonValue   { get; }
        public abstract     TypeDef     JsonKey     { get; }
        public abstract     TypeDef     ShortString { get; }
        public abstract     TypeDef     JsonEntity  { get; }
        public abstract     TypeDef     JsonTable   { get; }
    }
    
    /// <summary>
    /// Same value ids as in Friflo.Json.Fliox.Hub.Host.SQL.ColumnType
    /// </summary>
    public enum StandardTypeId
    {
        None        =  0,   // no mapper
        //
        Boolean     =  1,
        String      =  2,   // used also for ShortString
        // --- integer
        Uint8       =  3,
        Int16       =  4,
        Int32       =  5,
        Int64       =  6,
        // --- NON_CLS integer
        Int8        =  7,
        UInt16      =  8,
        UInt32      =  9,
        UInt64      = 10,
        // --- floating point
        Float       = 11,
        Double      = 12,
        // --- specialized
        BigInteger  = 13,
        DateTime    = 14,
        Guid        = 15,
        JsonValue   = 16,
        JsonKey     = 17,
        JsonEntity  = 19,
        Enum        = 20,
        JsonTable   = 21,
        //
        Object      = 22,   // set by mapper
        Array       = 23,   // set by mapper
        Dictionary  = 24,   // set by mapper
    }
    
    internal static class StandardTypeUtils
    {
        private static void AssertTypeId(TypeDef type, TypeDef standardType, StandardTypeId typeId) {
            if ((type == standardType) != (type.TypeId == typeId)) {
                throw new InvalidOperationException($"invalid typeId: {typeId} type: '{type}' standardType: '{standardType}'");
            }
        }
        
        [Conditional("DEBUG")]
        internal static void AssertTypeIds(IReadOnlyList<FieldDef> fields, StandardTypes standard)
        {
            foreach (var field in fields) {
                var type = field.type;
                AssertTypeId(type, standard.Boolean,    StandardTypeId.Boolean);
                // AssertTypeId(type, standard.String, StandardTypeId.String);
            
                AssertTypeId(type, standard.Uint8,      StandardTypeId.Uint8);
                AssertTypeId(type, standard.Int16,      StandardTypeId.Int16);
                AssertTypeId(type, standard.Int32,      StandardTypeId.Int32);
                AssertTypeId(type, standard.Int64,      StandardTypeId.Int64);
                
                // NON_CLS
                AssertTypeId(type, standard.Int8,       StandardTypeId.Int8);
                AssertTypeId(type, standard.UInt16,     StandardTypeId.UInt16);
                AssertTypeId(type, standard.UInt32,     StandardTypeId.UInt32);
                AssertTypeId(type, standard.UInt64,     StandardTypeId.UInt64);

                AssertTypeId(type, standard.Float,      StandardTypeId.Float);
                AssertTypeId(type, standard.Double,     StandardTypeId.Double);

                AssertTypeId(type, standard.BigInteger, StandardTypeId.BigInteger);
                AssertTypeId(type, standard.DateTime,   StandardTypeId.DateTime);
            
                AssertTypeId(type, standard.Guid,       StandardTypeId.Guid);
                AssertTypeId(type, standard.JsonValue,  StandardTypeId.JsonValue);
                AssertTypeId(type, standard.JsonKey,    StandardTypeId.JsonKey);
                AssertTypeId(type, standard.JsonEntity, StandardTypeId.JsonEntity);
                
                AssertTypeId(type, standard.JsonTable,  StandardTypeId.JsonTable);
            }
        }
    }
}