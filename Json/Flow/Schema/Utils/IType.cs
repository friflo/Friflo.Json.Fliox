// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
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
        public  abstract    ICollection<Field>  Fields       { get; }
        public  abstract    string              Discriminant { get; }
        public  abstract    TypeSemantic        TypeSemantic { get; }
        public  abstract    bool                IsNullable   { get; }
        public  abstract    bool                IsArray      { get; }
        public  abstract    ITyp                ElementType  { get; }
        public  abstract    bool                IsDictionary { get; }
        public  abstract    ICollection<string> GetEnumValues();
    }
    
    public class Field {
        public  string      jsonName;
        public  bool        required;
        public  ITyp        fieldType;
    }
    
    public class NativeType : ITyp
    {
        internal readonly   Type                native;
        private  readonly   TypeMapper          mapper;
        internal            ITyp                baseType;
        private  readonly   ICollection<Field>  fields;
        private             ITyp                elementType;
        
        public   override   string              Name            => native.Name;
        public   override   string              Namespace       => native.Namespace;
        public   override   ITyp                BaseType        => baseType;
        public   override   bool                IsEnum          => native.IsEnum;
        public   override   bool                IsComplex       => mapper.IsComplex;
        public   override   ICollection<Field>  Fields          => fields;
        public   override   string              Discriminant    => mapper.Discriminant;
        public   override   TypeSemantic        TypeSemantic    => mapper.GetTypeSemantic();
        public   override   bool                IsNullable      => mapper.isNullable;
        public   override   bool                IsArray         => mapper.IsArray;
        public   override   ITyp                ElementType     => elementType;
        public   override   bool                IsDictionary    => mapper.type.GetInterfaces().Contains(typeof(IDictionary));
        
        public   override   ICollection<string> GetEnumValues() => mapper.GetEnumValues();
           
        public NativeType (TypeMapper mapper) {
            this.native     = mapper.type;
            this.mapper     = mapper;
        }

        public override bool Equals(object obj) {
            if (obj == null)
                throw new NullReferenceException();
            var other = (NativeType)obj;
            return native == other.native;
        }

        public override int GetHashCode() {
            return (native != null ? native.GetHashCode() : 0);
        }
    }
}