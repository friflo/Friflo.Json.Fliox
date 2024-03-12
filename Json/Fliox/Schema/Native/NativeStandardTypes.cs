// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema.Native
{
    internal sealed class NativeStandardTypes : StandardTypes
    {
        public   override   TypeDef     Boolean     { get; }
        public   override   TypeDef     String      { get; }
        
        public   override   TypeDef     Uint8       { get; }
        public   override   TypeDef     Int16       { get; }
        public   override   TypeDef     Int32       { get; }
        public   override   TypeDef     Int64       { get; }
        
        // NON_CLS
        public   override   TypeDef     Int8        { get; }
        public   override   TypeDef     UInt16      { get; }
        public   override   TypeDef     UInt32      { get; }
        public   override   TypeDef     UInt64      { get; }
        
        public   override   TypeDef     Float       { get; }
        public   override   TypeDef     Double      { get; }
        public   override   TypeDef     BigInteger  { get; }
        public   override   TypeDef     DateTime    { get; }
        public   override   TypeDef     Guid        { get; }
        public   override   TypeDef     JsonValue   { get; }
        public   override   TypeDef     JsonKey     { get; }
        public   override   TypeDef     ShortString { get; }
        public   override   TypeDef     JsonEntity  { get; }
        public   override   TypeDef     JsonTable   { get; }
        
        internal NativeStandardTypes (Dictionary<Type, NativeTypeDef> types) {
            Boolean     = Find(types, typeof(bool));
            String      = Find(types, typeof(string));
            
            Uint8       = Find(types, typeof(byte));
            Int16       = Find(types, typeof(short));
            Int32       = Find(types, typeof(int));
            Int64       = Find(types, typeof(long));
            
            // NON_CLS
            Int8        = Find(types, typeof(sbyte));
            UInt16      = Find(types, typeof(ushort));
            UInt32      = Find(types, typeof(uint));
            UInt64      = Find(types, typeof(ulong));
            
            Float       = Find(types, typeof(float));
            Double      = Find(types, typeof(double));
            BigInteger  = Find(types, typeof(BigInteger));
            DateTime    = Find(types, typeof(DateTime));
            Guid        = Find(types, typeof(Guid));
            JsonValue   = Find(types, typeof(JsonValue));
            JsonKey     = Find(types, typeof(JsonKey));
            ShortString = Find(types, typeof(ShortString));
            JsonEntity  = Find(types, typeof(JsonEntity));
            JsonTable   = Find(types, typeof(JsonTable));
        }
        
        private static Dictionary<Type, StandardTypeInfo> GetTypes() {
            var map = new Dictionary<Type, StandardTypeInfo> {
                { typeof(bool),         Info("boolean",     StandardTypeId.Boolean)},
                { typeof(string),       Info("string",      StandardTypeId.String)},
                //
                { typeof(byte),         Info("uint8",       StandardTypeId.Uint8)},
                { typeof(short),        Info("int16",       StandardTypeId.Int16)},
                { typeof(int),          Info("int32",       StandardTypeId.Int32)},
                { typeof(long),         Info("int64",       StandardTypeId.Int64)},
                
                // NON_CLS
                { typeof(sbyte),        Info("int8",        StandardTypeId.Int8)},
                { typeof(ushort),       Info("uint16",      StandardTypeId.UInt16)},
                { typeof(uint),         Info("uint32",      StandardTypeId.UInt32)},
                { typeof(ulong),        Info("uint64",      StandardTypeId.UInt64)},
                //
                { typeof(float),        Info("float",       StandardTypeId.Float)},
                { typeof(double),       Info("double",      StandardTypeId.Double)},
                { typeof(BigInteger),   Info("BigInteger",  StandardTypeId.BigInteger)},
                { typeof(DateTime),     Info("DateTime",    StandardTypeId.DateTime)},
                { typeof(Guid),         Info("Guid",        StandardTypeId.Guid)},
                { typeof(JsonValue),    Info("JsonValue",   StandardTypeId.JsonValue)},
                { typeof(JsonKey),      Info("JsonKey",     StandardTypeId.JsonKey)},
                { typeof(ShortString),  Info("ShortString", StandardTypeId.String)},
                { typeof(JsonEntity),   Info("JsonEntity",  StandardTypeId.JsonEntity)},
                { typeof(JsonTable),    Info("JsonTable",   StandardTypeId.JsonTable)}
            };
            return map;
        }
        
        private static StandardTypeInfo Info(string name, StandardTypeId typeId) {
            return new StandardTypeInfo(name, typeId);
        }
        
        internal static readonly Dictionary<Type, StandardTypeInfo> Types = GetTypes();

        private static TypeDef Find (Dictionary<Type, NativeTypeDef> types, Type type) {
            if (types.TryGetValue(type, out var typeDef))
                return typeDef;
            return null;
        }
    }

    internal readonly struct StandardTypeInfo {
        internal    readonly    string          typeName;
        internal    readonly    StandardTypeId  typeId;
        
        internal StandardTypeInfo(string typeName, StandardTypeId typeId) {
            this.typeName   = typeName;
            this.typeId     = typeId;
        }
    }
}