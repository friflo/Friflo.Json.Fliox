// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema.JSON
{
    internal sealed class JsonStandardTypes : StandardTypes
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
        
        /// <summary>
        /// stand types are defined in <see cref="Language.JsonSchemaGenerator.GetStandardTypes"/> 
        /// </summary>
        internal JsonStandardTypes (Dictionary<string, JsonTypeDef> types, IUtf8Buffer buffer) {
            Boolean     = new JsonTypeDef("boolean",    buffer, StandardTypeId.Boolean);
            String      = new JsonTypeDef("string",     buffer, StandardTypeId.String);
            
            Uint8       = Find(types, "./Standard.json#/definitions/uint8");
            Int16       = Find(types, "./Standard.json#/definitions/int16");
            Int32       = Find(types, "./Standard.json#/definitions/int32");
            Int64       = Find(types, "./Standard.json#/definitions/int64");
            
            // NON_CLS
            Int8        = Find(types, "./Standard.json#/definitions/int8");
            UInt16      = Find(types, "./Standard.json#/definitions/uint16");
            UInt32      = Find(types, "./Standard.json#/definitions/uint32");
            UInt64      = Find(types, "./Standard.json#/definitions/uint64");
            
            Float       = Find(types, "./Standard.json#/definitions/float");
            Double      = Find(types, "./Standard.json#/definitions/double");
            BigInteger  = Find(types, "./Standard.json#/definitions/BigInteger");
            DateTime    = Find(types, "./Standard.json#/definitions/DateTime");
            Guid        = Find(types, "./Standard.json#/definitions/Guid");
            JsonValue   = new JsonTypeDef("{ }",        buffer, StandardTypeId.JsonValue);
            JsonKey     = Find(types, "./Standard.json#/definitions/JsonKey");
            ShortString = new JsonTypeDef("string",     buffer, StandardTypeId.String);
            JsonEntity  = new JsonTypeDef("{ }",        buffer, StandardTypeId.JsonEntity);
            JsonTable   = Find(types, "./Standard.json#/definitions/JsonTable"); // JSON_ARRAY
        }
        
        private static TypeDef Find (Dictionary<string, JsonTypeDef> types, string type) {
            if (types.TryGetValue(type, out var typeDef))
                return typeDef;
            return null;
        }
    }
}