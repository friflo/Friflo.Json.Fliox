// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Flow.Schema.Definition
{
    /// <summary>
    /// Contains the all required data to generate code for all types.
    /// Note: This file does and must not have any dependency to <see cref="System.Type"/>.
    /// </summary>
    public abstract class TypeSchema
    {
        public abstract     ICollection<TypeDef>    Types           { get; }
        public abstract     StandardTypes           StandardTypes   { get; }
        public abstract     ICollection<TypeDef>    SeparateTypes   { get; }
    }
    
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
        
        public abstract     TypeDef     JsonValue   { get; }
        
                
        public void SetStandardNames() {
            SetName(Boolean,       "boolean" );
            SetName(String,        "string" );

            SetName(Uint8,         "uint8" );
            SetName(Int16,         "int16" );
            SetName(Int32,         "int32" );
            SetName(Int64,         "int64" );
                
            SetName(Float,         "float" );
            SetName(Double,        "double" );
                
            SetName(BigInteger,    "BigInteger" );
            SetName(DateTime,      "DateTime" );
            
            SetName(JsonValue,     "JsonValue" );
        }
        
        private static void SetName (TypeDef type, string name) {
            if (type == null)
                return;
            type.Name       = name;
            type.Namespace  = "Standard";
        }
    }
}