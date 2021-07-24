// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper.Map;

// ReSharper disable ConvertToAutoProperty
namespace Friflo.Json.Flow.Schema.Utils
{
    // ReSharper disable once InconsistentNaming
    public abstract class ITyp {
        public  abstract    string              Name         { get; }
        public  abstract    string              Namespace    { get; }
        public  abstract    ITyp                BaseType     { get; }
        public  abstract    bool                IsEnum       { get; }
        public  abstract    bool                IsComplex    { get; }
        public  abstract    List<Field>         Fields       { get; }
        public  abstract    string              Discriminant { get; }
        public  abstract    TypeSemantic        TypeSemantic { get; }
        public  abstract    bool                IsNullable   { get; }
        public  abstract    bool                IsArray      { get; }
        public  abstract    ITyp                ElementType  { get; internal set; }
        public  abstract    bool                IsDictionary { get; }
        public  abstract    UnionType           UnionType    { get; }
        
        public  abstract    ICollection<string> GetEnumValues();
        public  abstract    bool                IsDerivedField(Field field);
    }
    
    public class Field {
        public              string      jsonName;
        public              bool        required;
        internal            ITyp        fieldType;

        public   override   string      ToString() => jsonName;
    }

    public class UnionType {
        public              string      discriminator;
        public              List<ITyp>  polyTypes;
        
        public   override   string      ToString() => discriminator;
    }
}