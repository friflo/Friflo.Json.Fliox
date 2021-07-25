// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Schema.Definition
{
    public abstract class TypeDef {
        public  abstract    string              Name         { get; }
        public  abstract    string              Namespace    { get; }
        public  abstract    TypeDef             BaseType     { get; }
        public  abstract    bool                IsEnum       { get; }
        public  abstract    bool                IsComplex    { get; }
        public  abstract    List<Field>         Fields       { get; }
        public  abstract    string              Discriminant { get; }
        public  abstract    TypeSemantic        TypeSemantic { get; }
        public  abstract    bool                IsArray      { get; }
        public  abstract    TypeDef             ElementType  { get; internal set; }
        public  abstract    bool                IsDictionary { get; }
        public  abstract    UnionType           UnionType    { get; }
        
        public  abstract    ICollection<string> GetEnumValues();
        public  abstract    bool                IsDerivedField(Field field);
    }
    
    public class Field {
        public              string          jsonName;
        public              bool            required;
        internal            TypeDef         fieldType;

        public   override   string          ToString() => jsonName;
    }

    public class UnionType {
        public              string          discriminator;
        public              List<TypeDef>   types;
        
        public   override   string          ToString() => discriminator;
    }
}