// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        public  abstract    ITyp                ElementType  { get; internal set; }
        public  abstract    bool                IsDictionary { get; }
        public  abstract    UnionType           UnionType    { get; }
        
        public  abstract    ICollection<string> GetEnumValues();
        public  abstract    bool                IsDerivedField(Field field);
    }
    
    public class Field {
        public   string     jsonName;
        public   bool       required;
        internal ITyp       fieldType;
    }
    
    public class NativeType : ITyp
    {
        internal readonly   Type                native;
        private  readonly   TypeMapper          mapper;
        internal            ITyp                baseType;
        private  readonly   List<Field>         fields;
        private             ITyp                elementType;
        private             UnionType           unionType;
        
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
        public   override   UnionType           UnionType       => UnionType;
        
        public   override   ITyp                ElementType {   get          => elementType;
                                                                internal set => elementType = value;  }

        public   override   bool                IsDictionary    => mapper.type.GetInterfaces().Contains(typeof(IDictionary));
        
        public   override   ICollection<string> GetEnumValues() => mapper.GetEnumValues();
        
        public   override   bool                IsDerivedField(Field field) {
            var parent = native.BaseType;
            while (parent != null) {
                if (fields.Find(f => f.jsonName == field.jsonName) != null)
                    return true;
                parent = parent.BaseType;
            }
            return false;    
        }
           
        public NativeType (TypeMapper mapper) {
            this.native     = mapper.type;
            this.mapper     = mapper;
            fields          = new List<Field>(mapper.propFields.fields.Length);
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
    
    public class UnionType {
        public  string      discriminator;
        public  List<ITyp>  polyTypes;
    }
}