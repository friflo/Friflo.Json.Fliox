// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Schema.Validation
{
    /// <summary>
    /// Similar to <see cref="Definition.TypeDef"/> but operates on byte arrays instead of strings to gain
    /// performance.
    /// </summary>
    public abstract class ValidationType {
        public              string                  Name         { get; }
        public              string                  Namespace    { get; }
        public              string                  Path         { get; internal set; }
        public  abstract    ValidationType          BaseType     { get; }
        public  abstract    bool                    IsComplex    { get; }
        public  abstract    bool                    IsStruct     { get; }
        public  abstract    List<ValidationField>   Fields       { get; }
        public  abstract    ValidationUnion         UnionType    { get; }
        public  abstract    bool                    IsAbstract   { get; }
        public  abstract    string                  Discriminant { get; }
        public  abstract    string                  Discriminator{ get; }
        public  abstract    bool                    IsEnum       { get; }
        public  abstract    ICollection<string>     EnumValues   { get; }
        public  abstract    TypeSemantic            TypeSemantic { get; }
        
        public  override    string                  ToString() => Name;
        
        protected ValidationType (string name, string @namespace) {
            Name        = name;
            Namespace   = @namespace;
        }
    }
    
    // could by a readonly struct - but may be used by reference in future 
    public class ValidationField {
        public  readonly    string          name;
        public  readonly    bool            required;
        public  readonly    ValidationType  type;
        public  readonly    bool            isArray;
        public  readonly    bool            isDictionary;
        public  readonly    ValidationType  ownerType;

        public  override    string          ToString() => name;
        
        public ValidationField(string name, bool required, ValidationType type, bool isArray, bool isDictionary, ValidationType ownerType) {
            this.name           = name;
            this.required       = required;
            this.type           = type;
            this.isArray        = isArray;
            this.isDictionary   = isDictionary;
            this.ownerType      = ownerType;
        }
        
        public bool IsDerivedField { get {
            var parent = ownerType.BaseType;
            while (parent != null) {
                foreach (var field in parent.Fields) {
                    if (field.name == name)
                        return true;
                }
                parent = parent.BaseType;
            }
            return false;
        }}
    }

    public class ValidationUnion {
        public  readonly    string                  discriminator;
        public  readonly    List<ValidationType>    types;
        
        public   override   string                  ToString() => discriminator;
        
        public ValidationUnion(string discriminator, List<ValidationType> types) {
            this.discriminator  = discriminator;
            this.types          = types;
        }
    }
}