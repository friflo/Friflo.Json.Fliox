// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema.Validation
{
    public enum TypeId
    {
        None,
        // --- object types
        Complex,
        Union,
        // --- number types
        Uint8,
        Int16,
        Int32,
        Int64,
        Float,
        Double,
        Boolean,   
        // --- string types        
        String,
        BigInteger,
        DateTime,
        Enum,
        //
        JsonValue 
    }
    
    /// <summary>
    /// Similar to <see cref="Definition.TypeDef"/> but operates on byte arrays instead of strings to gain
    /// performance.
    /// </summary>
    public sealed class ValidationType : IDisposable {
        private  readonly   TypeDef             typeDef;    // only for debugging
        private  readonly   string              name;       // only for debugging
        private  readonly   string              @namespace; // only for debugging
        public   readonly   TypeId              typeId;
        public   readonly   ValidationField[]   fields;
        public   readonly   ValidationUnion     unionType;
        public   readonly   Bytes[]             enumValues;
        
        public  override    string              ToString() => $"{typeId} - {@namespace}.{name}";
        
        public ValidationType (TypeId typeId, TypeDef typeDef) {
            this.typeId     = typeId;
            this.typeDef    = typeDef;
            this.name       = typeDef.Name;
            this.@namespace = typeDef.Namespace;
        }

        public ValidationType (TypeDef typeDef) {
            this.typeDef    = typeDef;     
            name            = typeDef.Name;
            @namespace      = typeDef.Namespace;
            
            var union = typeDef.UnionType;
            if (union != null) {
                typeId          = TypeId.Union;
                unionType       = new ValidationUnion(union);
                return;
            }
            if (typeDef.IsComplex) {
                typeId = TypeId.Complex;
                var typeField = typeDef.Fields;
                fields = new ValidationField[typeField.Count];
                int n = 0;
                foreach (var field in typeField) {
                    var validationField = new ValidationField(field);
                    fields[n++] = validationField;
                }
                return;
            }
            if (typeDef.IsEnum) {
                typeId = TypeId.Enum;
                var typeEnums   = typeDef.EnumValues;
                enumValues = new Bytes[typeEnums.Count];
                int n = 0;
                foreach (var enumValue in typeEnums) {
                    enumValues[n++] = new Bytes(enumValue);
                }
                return;
            }
            throw new InvalidOperationException($"unhandled typeDef: {typeDef}");
        }
        
        public void Dispose() {
            if (enumValues != null) {
                foreach (var enumValue in enumValues) {
                    enumValue.Dispose();
                }
            }
            if (fields != null) {
                foreach (var field in fields) {
                    field.Dispose();
                }
            }
            unionType?.Dispose();
        }
    }
    
    // could by a struct 
    public class ValidationField : IDisposable {
        public   readonly   string          fieldName;
        public              Bytes           name;
        public   readonly   bool            required;
        public   readonly   bool            isArray;
        public   readonly   bool            isDictionary;
    
        // --- internal
        internal            ValidationType  type;
        internal            TypeId          typeId;
        internal readonly   TypeDef         typeDef;

        public  override    string          ToString() => fieldName;
        
        public ValidationField(FieldDef fieldDef) {
            typeDef         = fieldDef.type;
            fieldName       = fieldDef.name;
            name            = new Bytes(fieldDef.name);
            required        = fieldDef.required;
            isArray         = fieldDef.isArray;
            isDictionary    = fieldDef.isDictionary;
        }
        
        public void Dispose() {
            name.Dispose();
        }
    }

    public class ValidationUnion : IDisposable {
        public  readonly    UnionType       unionType;
        public  readonly    string          discriminatorStr;
        public              Bytes           discriminator;
        public  readonly    UnionItem[]     types;
        
        public   override   string          ToString() => discriminatorStr;

        public ValidationUnion(UnionType union) {
            unionType           = union;
            discriminatorStr    = union.discriminator;
            discriminator       = new Bytes(union.discriminator);
            types               = new UnionItem[union.types.Count];
        }
        
        public void Dispose() {
            discriminator.Dispose();
            foreach (var type in types) {
                type.discriminant.Dispose();
            }
        }
    }
    
    public struct UnionItem
    {
        private  readonly   string          discriminantStr;
        internal            Bytes           discriminant;
        public   readonly   ValidationType  type;

        public   override   string          ToString() => discriminantStr;

        public UnionItem (string discriminant, ValidationType type) {
            discriminantStr     = discriminant;
            this.discriminant   = new Bytes(discriminant);
            this.type           = type;
        }
    }
}