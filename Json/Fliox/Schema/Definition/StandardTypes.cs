// Copyright (c) Ullrich Praetz. All rights reserved.
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
        None        = 0,
        //
        Boolean     = 1,
        String      = 2,    // used also for ShortString
        // --- integer
        Uint8       = 3,
        Int16       = 4,
        Int32       = 5,
        Int64       = 6,
        // --- floating point
        Float       = 7,
        Double      = 8,
        // --- specialized
        BigInteger  = 9,
        DateTime    = 10,
        Guid        = 11,
        JsonValue   = 12,
        JsonKey     = 13,
        JsonEntity  = 15,
        Enum        = 16,
        JsonTable   = 17,
        //
        Object      = 18,
        Array       = 19,
        Dictionary  = 20,
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