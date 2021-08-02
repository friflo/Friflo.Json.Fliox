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
        public  readonly    string                  name;       // only for debugging
        public  readonly    string                  @namespace; // only for debugging
    //  public              string                  Path        { get; internal set; }
    //  public              ValidationType          BaseType    { get; }
        public  readonly    bool                    isComplex;
    //  public              bool                    IsStruct    { get; }
        public  readonly    List<ValidationField>   fields;
        public  readonly    ValidationUnion         unionType;
    //  public              bool                    IsAbstract  { get; }
        public              Bytes                   discriminant;
        public              Bytes                   discriminator;
        public  readonly    bool                    isEnum;
        public  readonly    List<Bytes>             enumValues;
    //  public              TypeSemantic            typeSemantic;
        
        public  override    string                  ToString() => $"{@namespace}.{name}";

        public ValidationType (TypeDef typeDef) {
            name            = typeDef.Name;
            @namespace      = typeDef.Namespace;
            isComplex       = typeDef.IsComplex;
            isEnum          = typeDef.IsEnum;
            discriminant    = new Bytes(typeDef.Discriminant);
            discriminator   = new Bytes(typeDef.Discriminator);
            var typeEnums   = typeDef.EnumValues;
            if (typeEnums != null) {
                enumValues = new List<Bytes>(typeEnums.Count);
                foreach (var enumValue in typeEnums) {
                    enumValues.Add(new Bytes(enumValue));
                }
            }
            var typeField = typeDef.Fields;
            if (typeField != null) {
                fields = new List<ValidationField>(typeField.Count);
                foreach (var field in typeField) {
                    var validationField = new ValidationField(field);
                    fields.Add(validationField);
                }
            }
            var union = typeDef.UnionType;
            if (union != null) {
                unionType = new ValidationUnion(union);
            }
        }
        
        public void Dispose() {
            discriminant.Dispose();
            discriminator.Dispose();
            foreach (var enumValue in enumValues) {
                enumValue.Dispose();
            }
            foreach (var field in fields) {
                field.Dispose();
            }
            unionType?.Dispose();
        }
    }
    
    // could by a struct 
    public class ValidationField : IDisposable {
        public              Bytes           name;
        public  readonly    bool            required;
        public              ValidationType  Type => type;
        public  readonly    bool            isArray;
        public  readonly    bool            isDictionary;
    //  public  readonly    ValidationType  ownerType;
    
        // --- internal
        internal            ValidationType  type;

        public  override    string          ToString() => name.ToString();
        
        public ValidationField(FieldDef fieldDef) {
            name           = new Bytes(fieldDef.name);
            required       = fieldDef.required;
            isArray        = fieldDef.isArray;
            isDictionary   = fieldDef.isDictionary;
        }
        
        public void Dispose() {
            name.Dispose();
        }
    }

    public class ValidationUnion : IDisposable {
        public              Bytes                   discriminator;
        public  readonly    List<ValidationType>    types;
        
        public   override   string                  ToString() => discriminator.ToString();

        public ValidationUnion(UnionType union) {
            discriminator   = new Bytes(union.discriminator);
            types           = new List<ValidationType>(union.types.Count);
        }
        
        public void Dispose() {
            discriminator.Dispose();
        }
    }
}