// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
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
        // ReSharper disable once NotAccessedField.Local
        private  readonly   TypeDef             typeDef;    // only for debugging
        public   readonly   string              name;
        private  readonly   string              qualifiedName;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        public   readonly   string              @namespace;
        public   readonly   TypeId              typeId;
        private  readonly   ValidationField[]   fields;
        public   readonly   int                 requiredFieldsCount;
        private  readonly   ValidationField[]   requiredFields;
        public   readonly   ValidationUnion     unionType;
        private  readonly   Bytes[]             enumValues;
        
        public  override    string              ToString() => qualifiedName;
        
        internal ValidationType (TypeId typeId, string typeName, TypeDef typeDef) {
            this.typeId         = typeId;
            this.typeDef        = typeDef;
            this.name           = typeName;
            this.@namespace     = typeDef.Namespace;
            this.qualifiedName  = $"{@namespace}.{name}";
        }
        
        private ValidationType (TypeDef typeDef, UnionType union)               : this (TypeId.Union, typeDef.Name, typeDef) {
            unionType       = new ValidationUnion(union);
        }
        
        private ValidationType (TypeDef typeDef, List<FieldDef> fieldDefs)      : this (TypeId.Class, typeDef.Name, typeDef) {
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
        
        private ValidationType (TypeDef typeDef, ICollection<string> typeEnums) : this (TypeId.Enum, typeDef.Name, typeDef) {
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
        
        public static string GetName (ValidationType type, bool qualified) {
            var typeId = type.typeId; 
            if (typeId == TypeId.Class || typeId == TypeId.Union|| typeId == TypeId.Enum) {
                if (qualified) {
                    return type.qualifiedName;
                }
                return type.name;
            }
            return type.name;
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
        
        internal static bool FindEnum (ValidationType type, ref Bytes value, TypeValidator validator, ValidationType parent) {
            var enumValues = type.enumValues;
            for (int n = 0; n < enumValues.Length; n++) {
                if (enumValues[n].IsEqual(ref value)) {
                    return true;
                }
            }
            return validator.ErrorType("Invalid enum value.", value.ToString(), true, type.name, type.@namespace, parent);
        }
        
        internal static bool FindField (ValidationType type, TypeValidator validator, out ValidationField field, bool[] foundFields) {
            ref var parser = ref validator.parser;
            foreach (var typeField in type.fields) {
                if (!parser.key.IsEqual(ref typeField.name))
                    continue;
                field   = typeField;
                var reqPos = field.requiredPos;
                if (reqPos >= 0) {
                    foundFields[reqPos] = true;
                }
                var ev = parser.Event; 
                if (ev != JsonEvent.ArrayStart && ev != JsonEvent.ValueNull && field.isArray) {
                    var value       = GetValue(ref parser, out bool isString);
                    validator.ErrorType("Incorrect type.", value, isString, field.typeName, field.type.@namespace, type);
                    return false;
                }
                return true;
            }
            validator.ErrorValue("Unknown property:", parser.key.ToString(), true, type);
            field = null;
            return false;
        }
        
        private static string GetValue(ref JsonParser parser, out bool isString) {
            isString = false;
            switch (parser.Event) {
                case JsonEvent.ValueString:     isString = true;
                                                return parser.value.ToString();
                case JsonEvent.ObjectStart:     return "object";
                case JsonEvent.ValueNumber:     return parser.value.ToString();
                case JsonEvent.ValueBool:       return parser.boolValue ? "true" : "false";
                case JsonEvent.ValueNull:       return "null";
                default:
                    return parser.Event.ToString();
            }
        }

        public bool HasMissingFields(bool[] foundFields, StringBuilder sb) {
            var foundCount = 0;
            for (int n = 0; n < requiredFieldsCount; n++) {
                if (foundFields[n])
                    foundCount++;
            }
            var missingCount = requiredFieldsCount - foundCount;
            if (missingCount == 0) {
                return false;
            }
            bool first = true;
            sb.Clear();
            sb.Append('[');
            for (int n = 0; n < requiredFieldsCount; n++) {
                if (!foundFields[n]) {
                    if (first) {
                        first = false;
                    } else {
                        sb.Append(", ");
                    }
                    var fieldName = requiredFields[n].fieldName;
                    sb.Append(fieldName);
                }
            }
            sb.Append(']');
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
        internal readonly   string          typeName;

        public  override    string          ToString() => fieldName;
        
        public ValidationField(FieldDef fieldDef, int requiredPos) {
            typeDef             = fieldDef.type;
            typeName            = fieldDef.isArray ? $"{typeDef.Name}[]" : typeDef.Name; 
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
        public              string      TypesAsString { get; private set; }

        public   override   string      ToString()      => discriminatorStr;

        public ValidationUnion(UnionType union) {
            unionType           = union;
            discriminatorStr    = $"'{union.discriminator}'";
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
            TypesAsString       = GetTypesAsString();
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
        
        private string GetTypesAsString() {
            var sb = new StringBuilder();
            bool first = true;
            sb.Clear();
            sb.Append('[');
            foreach (var type in types) {
                if (first) {
                    first = false;
                } else {
                    sb.Append(", ");
                }
                sb.Append(type.discriminantStr);
            }
            sb.Append(']');
            return sb.ToString();
        }
    }
    
    public struct UnionItem
    {
        internal readonly   string          discriminantStr;
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