// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema.Validation
{
    /// <summary>
    /// Similar to <see cref="Definition.TypeDef"/> but operates on byte arrays instead of strings to gain
    /// performance.
    /// </summary>
    public sealed class ValidationType : IDisposable {
        public              Bytes                   name; 
        public  readonly    string                  @namespace; // only for debugging
    //  public              string                  Path         { get; internal set; }
    //  public              ValidationType          BaseType     { get; }
        public  readonly    bool                    isComplex;
    //  public              bool                    IsStruct     { get; }
        public  readonly    List<ValidationField>   fields;
        public  readonly    ValidationUnion         unionType;
    //  public              bool                    IsAbstract   { get; }
        public              Bytes                   discriminant;
        public              Bytes                   discriminator;
        public  readonly    bool                    isEnum;
        public  readonly    List<Bytes>             enumValues;
    //  public              TypeSemantic            typeSemantic;
        
        public  override    string                  ToString() => name.ToString();

        public ValidationType (TypeDef typeDef) {
            name            = new Bytes(typeDef.Name);
            @namespace      = typeDef.Namespace;
            isComplex       = typeDef.IsComplex;
            isEnum          = typeDef.IsEnum;
            discriminant    = new Bytes(typeDef.Discriminant);
            discriminator   = new Bytes(typeDef.Discriminator);
            if (typeDef.EnumValues != null) {
                enumValues = new List<Bytes>(typeDef.EnumValues.Count);
                foreach (var enumValue in typeDef.EnumValues) {
                    enumValues.Add(new Bytes(enumValue));
                }
            }
            if (typeDef.Fields != null) {
                fields = new List<ValidationField>(typeDef.Fields.Count);
                foreach (var field in typeDef.Fields) {
                    fields.Add(null); // todo
                }
            }
            var union = typeDef.UnionType;
            if (union != null) {
                unionType = new ValidationUnion(union.discriminator, null); // todo
            }
        }
        
        public void Dispose() {
            name.Dispose();
            discriminant.Dispose();
            discriminator.Dispose();
            foreach (var enumValue in enumValues) {
                enumValue.Dispose();
            }
            foreach (var field in fields) {
                field.Dispose();
            }
        }
    }
    
    // could by a struct 
    public class ValidationField : IDisposable {
        public              Bytes           name;
        public  readonly    bool            required;
        public  readonly    ValidationType  type;
        public  readonly    bool            isArray;
        public  readonly    bool            isDictionary;
        public  readonly    ValidationType  ownerType;

        public  override    string          ToString() => name.ToString();
        
        public ValidationField(string name, bool required, ValidationType type, bool isArray, bool isDictionary, ValidationType ownerType) {
            this.name           = new Bytes(name);
            this.required       = required;
            this.type           = type;
            this.isArray        = isArray;
            this.isDictionary   = isDictionary;
            this.ownerType      = ownerType;
        }
        
        public void Dispose() {
            name.Dispose();
        }
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