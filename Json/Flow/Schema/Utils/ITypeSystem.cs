// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Flow.Schema.Utils
{
    public interface ITypeSystem
    {
        ICollection<TypeDef> Types { get;}
        
        TypeDef     Boolean     { get; }
        TypeDef     String      { get; }
        
        TypeDef     Unit8       { get; }
        TypeDef     Int16       { get; }
        TypeDef     Int32       { get; }
        TypeDef     Int64       { get; }
        
        TypeDef     Float       { get; }
        TypeDef     Double      { get; }
        
        TypeDef     BigInteger  { get; }
        TypeDef     DateTime    { get; }
        
        TypeDef     JsonValue   { get; }
    }
}