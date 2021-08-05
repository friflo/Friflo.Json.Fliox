// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema.Validation
{
    public enum TypeId
    {
        None,
        // --- object types
        Class,
        Union,
        // --- number types
        Uint8,
        Int16,
        Int32,
        Int64,
        Float,
        Double,
        // --- boolean type
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
        public   readonly   string              name;       // only for debugging
        private  readonly   string              @namespace; // only for debugging
        public   readonly   TypeId              typeId;
        private  readonly   ValidationField[]   fields;
        public   readonly   int                 requiredFieldsCount;
        private  readonly   ValidationField[]   requiredFields;
        public   readonly   ValidationUnion     unionType;
        private  readonly   Bytes[]             enumValues;
        
        public  override    string              ToString() => $"{@namespace}.{name}";
        
        internal ValidationType (TypeId typeId, TypeDef typeDef) {
            this.typeId     = typeId;
            this.typeDef    = typeDef;
            this.name       = typeDef.Name;
            this.@namespace = typeDef.Namespace;
        }
        
        private ValidationType (TypeDef typeDef, UnionType union)               : this (TypeId.Union,   typeDef) {
            unionType       = new ValidationUnion(union);
        }
        
        private ValidationType (TypeDef typeDef, List<FieldDef> fieldDefs)      : this (TypeId.Class, typeDef) {
            int requiredCount = 0;
            foreach (var field in fieldDefs) {
                if (field.required)
                    requiredCount++;
            }
            requiredFieldsCount = requiredCount;
            requiredFields      = new ValidationField[requiredCount];
            fields              = new ValidationField[fieldDefs.Count];
            int n = 0;
            int requiredPos = 0;
            foreach (var field in fieldDefs) {
                var reqPos = field.required ? requiredPos++ : -1;
                var validationField = new ValidationField(field, reqPos);
                fields[n++] = validationField;
                if (reqPos >= 0)
                    requiredFields[reqPos] = validationField;
            }
        }
        
        private ValidationType (TypeDef typeDef, ICollection<string> typeEnums) : this (TypeId.Enum,    typeDef) {
            enumValues = new Bytes[typeEnums.Count];
            int n = 0;
            foreach (var enumValue in typeEnums) {
                enumValues[n++] = new Bytes(enumValue);
            }
        }

        public static ValidationType Create (TypeDef typeDef) {
            var union = typeDef.UnionType;
            if (union != null) {
                return new ValidationType(typeDef, union);
            }
            if (typeDef.IsClass) {
                return new ValidationType(typeDef, typeDef.Fields);
            }
            if (typeDef.IsEnum) {
                return new ValidationType(typeDef, typeDef.EnumValues);
            }
            return null;
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
        
        internal void SetFields(Dictionary<TypeDef, ValidationType> typeMap) {
            if (fields != null) {
                foreach (var field in fields) {
                    var fieldType   = typeMap[field.typeDef];
                    field.type      = fieldType;
                    field.typeId    = fieldType.typeId;
                }
            }
        }
        
        internal static bool FindEnum (ValidationType type, ref Bytes value, out string msg) {
            var enumValues = type.enumValues;
            for (int n = 0; n < enumValues.Length; n++) {
                if (enumValues[n].IsEqual(ref value)) {
                    msg = null;
                    return true;
                }
            }
            msg = $"Incorrect enum value: '{value}'";
            return false;
        }
        
        internal static bool FindField (ValidationType type, ref Bytes key, out ValidationField field, out string msg, bool[] foundFields) {
            foreach (var typeField in type.fields) {
                if (key.IsEqual(ref typeField.name)) {
                    field   = typeField;
                    msg = null;
                    var reqPos = field.requiredPos;
                    if (reqPos >= 0) {
                        foundFields[reqPos] = true;
                    }
                    return true;
                }
            }
            msg = $"Field not found. key: '{key}'";
            field = null;
            return false;
        }

        public bool HasMissingFields(bool[] foundFields, out string[] missingFields) {
            var foundCount = 0;
            for (int n = 0; n < requiredFieldsCount; n++) {
                if (foundFields[n])
                    foundCount++;
            }
            var missingCount = requiredFieldsCount - foundCount;
            if (missingCount == 0) {
                missingFields = null;
                return false;
            }
            missingFields = new string [missingCount];
            int missingFieldPos = 0;
            
            for (int n = 0; n < requiredFieldsCount; n++) {
                if (!foundFields[n])
                    missingFields[missingFieldPos++] = requiredFields[n].fieldName;
            }
            return true;
        }
    }
    
    // could by a struct 
    public class ValidationField : IDisposable {
        public   readonly   string          fieldName;
        public              Bytes           name;
        public   readonly   bool            required;
        public   readonly   bool            isArray;
        public   readonly   bool            isDictionary;
        public   readonly   int             requiredPos;
    
        // --- internal
        internal            ValidationType  type;
        internal            TypeId          typeId;
        internal readonly   TypeDef         typeDef;

        public  override    string          ToString() => fieldName;
        
        public ValidationField(FieldDef fieldDef, int requiredPos) {
            typeDef             = fieldDef.type;
            fieldName           = fieldDef.name;
            name                = new Bytes(fieldDef.name);
            required            = fieldDef.required;
            isArray             = fieldDef.isArray;
            isDictionary        = fieldDef.isDictionary;
            this.requiredPos    = requiredPos;
        }
        
        public void Dispose() {
            name.Dispose();
        }
    }

    public class ValidationUnion : IDisposable {
        private  readonly   UnionType   unionType;
        public   readonly   string      discriminatorStr;
        public              Bytes       discriminator;
        private  readonly   UnionItem[] types;
        
        public   override   string      ToString() => discriminatorStr;

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

        internal void SetUnionTypes(Dictionary<TypeDef, ValidationType> typeMap) {
            int n = 0;
            foreach (var typeDef in unionType.types) {
                ValidationType validationType = typeMap[typeDef];
                var item = new UnionItem(typeDef.Discriminant, validationType);
                types[n++] = item;
            }
        }
        
        internal static bool FindUnion (ValidationUnion union, ref Bytes discriminant, out ValidationType type) {
            var types = union.types;
            for (int n = 0; n < types.Length; n++) {
                if (discriminant.IsEqual(ref types[n].discriminant)) {
                    type    = types[n].type;
                    return true;
                }
            }
            type    = null;
            return false;
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