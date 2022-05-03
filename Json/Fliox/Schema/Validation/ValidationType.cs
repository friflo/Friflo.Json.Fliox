// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema.Validation
{
    internal enum TypeId
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
        Guid,
        Enum,
        //
        JsonValue 
    }
    
    /// <summary>
    /// Similar to <see cref="Definition.TypeDef"/> but operates on byte arrays instead of strings to gain
    /// performance.
    /// </summary>
    public sealed class ValidationType  {
        public    readonly  string              name;
        public    readonly  string              @namespace;
        
        // --- intern / private
        // ReSharper disable once NotAccessedField.Local
        private   readonly  TypeDef             typeDef;    // only for debugging
        private   readonly  string              qualifiedName;
        internal  readonly  TypeId              typeId;
        private   readonly  ValidationField[]   fields;
        internal  readonly  int                 requiredFieldsCount;
        private   readonly  ValidationField[]   requiredFields;
        internal  readonly  ValidationUnion     unionType;
        private   readonly  Utf8String[]        enumValues;
        
        public              IEnumerable<ValidationField>    Fields      => fields;
        public   override   string                          ToString()  => qualifiedName;
        
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
        
        private ValidationType (TypeDef typeDef, IReadOnlyList<FieldDef> fieldDefs)      : this (TypeId.Class, typeDef.Name, typeDef) {
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
        
        private ValidationType (TypeDef typeDef, IReadOnlyList<EnumValue> typeEnums) : this (TypeId.Enum, typeDef.Name, typeDef) {
            enumValues = new Utf8String[typeEnums.Count];
            int n = 0;
            foreach (var enumValue in typeEnums) {
                enumValues[n++] = enumValue.nameUtf8;
            }
        }

        internal static ValidationType Create (TypeDef typeDef) {
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
        
        internal static string GetName (ValidationType type, bool qualified) {
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
            return validator.ErrorType("Invalid enum value.", value.AsString(), true, type.name, type.@namespace, parent);
        }
        
        internal static bool FindField (ValidationType type, TypeValidator validator, out ValidationField field, bool[] foundFields) {
            ref var parser = ref validator.parser;
            foreach (var typeField in type.fields) {
                if (!typeField.name.IsEqual(ref parser.key))
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
            validator.ErrorValue("Unknown property:", parser.key.AsString(), true, type);
            field = null;
            return false;
        }
        
        private static string GetValue(ref Utf8JsonParser parser, out bool isString) {
            isString = false;
            switch (parser.Event) {
                case JsonEvent.ValueString:     isString = true;
                                                return parser.value.AsString();
                case JsonEvent.ObjectStart:     return "object";
                case JsonEvent.ValueNumber:     return parser.value.AsString();
                case JsonEvent.ValueBool:       return parser.boolValue ? "true" : "false";
                case JsonEvent.ValueNull:       return "null";
                default:
                    return parser.Event.ToString();
            }
        }

        internal bool HasMissingFields(bool[] foundFields, StringBuilder sb) {
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
}