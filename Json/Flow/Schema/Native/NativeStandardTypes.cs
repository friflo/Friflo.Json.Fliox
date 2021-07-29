// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Friflo.Json.Flow.Mapper.Map.Val;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema.Native
{
    public class NativeStandardTypes : StandardTypes
    {
        public   override   TypeDef     Boolean     { get; }
        public   override   TypeDef     String      { get; }
        public   override   TypeDef     Uint8       { get; }
        public   override   TypeDef     Int16       { get; }
        public   override   TypeDef     Int32       { get; }
        public   override   TypeDef     Int64       { get; }
        public   override   TypeDef     Float       { get; }
        public   override   TypeDef     Double      { get; }
        public   override   TypeDef     BigInteger  { get; }
        public   override   TypeDef     DateTime    { get; }
        public   override   TypeDef     JsonValue   { get; }
        
        internal NativeStandardTypes (Dictionary<Type, NativeTypeDef> types) {
            Boolean     = Find(types, typeof(bool));
            String      = Find(types, typeof(string));
            Uint8       = Find(types, typeof(byte));
            Int16       = Find(types, typeof(short));
            Int32       = Find(types, typeof(int));
            Int64       = Find(types, typeof(long));
            Float       = Find(types, typeof(float));
            Double      = Find(types, typeof(double));
            BigInteger  = Find(types, typeof(BigInteger));
            DateTime    = Find(types, typeof(DateTime));
            JsonValue   = Find(types, typeof(JsonValue));
        }
        
        private static Dictionary<Type, string> GetTypes() {
            var map = new Dictionary<Type, string> {
                { typeof(bool),         "boolean"},
                { typeof(string),       "string"},
                { typeof(byte),         "uint8"},
                { typeof(short),        "int16"},
                { typeof(int),          "int32"},
                { typeof(long),         "int64"},
                { typeof(float),        "float"},
                { typeof(double),       "double"},
                { typeof(BigInteger),   "BigInteger"},
                { typeof(DateTime),     "DateTime"},
                { typeof(JsonValue),    "JsonValue"}
            };
            return map;
        }
        
        internal static readonly Dictionary<Type, string> Types = GetTypes();

        private static TypeDef Find (Dictionary<Type, NativeTypeDef> types, Type type) {
            if (types.TryGetValue(type, out var typeDef))
                return typeDef;
            return null;
        }
    }
}