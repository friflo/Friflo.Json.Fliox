// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Flow.Schema.Definition
{
    public abstract class TypeSchema
    {
        public abstract     ICollection<TypeDef> Types { get;}
        
        public abstract     TypeDef     Boolean     { get; }
        public abstract     TypeDef     String      { get; }
        
        public abstract     TypeDef     Unit8       { get; }
        public abstract     TypeDef     Int16       { get; }
        public abstract     TypeDef     Int32       { get; }
        public abstract     TypeDef     Int64       { get; }
        
        public abstract     TypeDef     Float       { get; }
        public abstract     TypeDef     Double      { get; }
        
        public abstract     TypeDef     BigInteger  { get; }
        public abstract     TypeDef     DateTime    { get; }
        
        public abstract     TypeDef     JsonValue   { get; }
    }
}