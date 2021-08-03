// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Flow.Schema.Validation
{
    public class JsonValidator : IDisposable
    {
        private Bytes               jsonBytes = new Bytes(128);
        private string              error;
        
        public bool Validate (ref JsonParser parser, string json, ValidationType type, out string error) {
            jsonBytes.Clear();
            jsonBytes.AppendString(json);
            parser.InitParser(jsonBytes);
            var ev      = parser.NextEvent();
            if (ValidateObject(ref parser, type)) {
                error = null;
                return true;
            }
            error = this.error;
            return false;
        }
        
        private bool ValidateObject (ref JsonParser parser, ValidationType type)
        {
            TypeId typeId;
            if (type.typeId == TypeId.Union) {
                var ev      = parser.NextEvent();
                if (ev != JsonEvent.ValueString) {
                    return Error("Expect discriminator string first member");
                }
                var unionType = type.unionType;
                if (!parser.key.IsEqual(ref unionType.discriminator)) {
                    return Error($"Unexpected discriminator name. was: {parser.key}, expect: {unionType.discriminator}");
                }
                if (!FindUnion(unionType, ref parser.value, out type, out typeId)) {
                    return Error($"unknown discriminator: {parser.key}");
                }
            }
            while (true) {
                var ev      = parser.NextEvent();
                ValidationField field;
                switch (ev) {
                    case JsonEvent.ValueString:
                        if (!FindField(type, ref parser.key, out field, out typeId))
                            return false;
                        switch (typeId) {
                            case TypeId.String:
                            case TypeId.BigInteger:
                            case TypeId.DateTime:
                                continue;
                            default:
                                return Error($"Found string but expect: {typeId}, field: {field}");
                        }
                        
                    case JsonEvent.ValueNumber:
                        if (!FindField(type, ref parser.key, out field, out typeId))
                            return false;
                        switch (typeId) {
                            case TypeId.Uint8:
                            case TypeId.Int16:
                            case TypeId.Int32:
                            case TypeId.Int64:
                            case TypeId.Float:
                            case TypeId.Double:
                                continue;
                            default:
                                return Error($"Found number but expect: {typeId}, field: {field}");
                        }
                        
                    case JsonEvent.ValueBool:
                        if (!FindField(type, ref parser.key, out field, out typeId))
                            return false;
                        if (typeId == TypeId.Boolean)
                            continue;
                        return Error($"Found boolean but expect: {typeId}, field: {field}");
                    
                    case JsonEvent.ValueNull:
                        if (!FindField(type, ref parser.key, out field, out typeId))
                            return false;
                        if (!field.required)
                            continue;
                        return Error($"Found null for a required field: {field}");
                    
                    case JsonEvent.ArrayStart:
                        if (!FindField(type, ref parser.key, out field, out typeId))
                            return false;
                        if (field.isArray) {
                            if (ValidateElement (ref parser, field.type, field.fieldName, true))
                                continue;
                            return false;
                        }
                        return Error($"Found array but expect: {typeId}, field: {field}");
                    
                    case JsonEvent.ObjectStart:
                        if (!FindField(type, ref parser.key, out field, out typeId))
                            return false;
                        if (typeId == TypeId.Complex) {
                            if (field.isDictionary) {
                                if (ValidateElement (ref parser, field.type, field.fieldName, false))
                                    continue;
                                return false;
                            }
                            if (ValidateElement (ref parser, field.type, field.fieldName, true))
                                continue;
                            return false;
                        }
                        return Error($"Found object but expect: {typeId}, field: {field}");
                    
                    case JsonEvent.Error:
                        return Error(parser.error.msg.ToString());
                    
                    case JsonEvent.ObjectEnd:
                        return true;
                        /* ev = parser.NextEvent();
                        if (ev == JsonEvent.EOF) {
                            error = null;
                            return true;
                        }
                        return Error("Expected EOF in JSON value"); */
                    case JsonEvent.ArrayEnd:
                        throw new InvalidOperationException($"unexpected event: {ev}");
                }
            }
        }
        
        private bool ValidateElement (ref JsonParser parser, ValidationType type, string fieldName, bool isArray) {
            while (true) {
                var ev      = parser.NextEvent();
                TypeId typeId = type.typeId;
                switch (ev) {
                    case JsonEvent.ValueString:
                        switch (typeId) {
                            case TypeId.String:
                            case TypeId.BigInteger:
                            case TypeId.DateTime:
                                continue;
                            case TypeId.Enum:
                                if (FindEnum(type, ref parser.value))
                                    continue;
                                return false;
                            default:
                                return Error($"Found string but expect: {typeId}, field: {fieldName}");
                        }
                        
                    case JsonEvent.ValueNumber:
                        switch (typeId) {
                            case TypeId.Uint8:
                            case TypeId.Int16:
                            case TypeId.Int32:
                            case TypeId.Int64:
                            case TypeId.Float:
                            case TypeId.Double:
                                continue;
                            default:
                                return Error($"Found number but expect: {typeId}, field: {fieldName}");
                        }
                        
                    case JsonEvent.ValueBool:
                        if (typeId == TypeId.Boolean)
                            continue;
                        return Error($"Found boolean but expect: {typeId}, field: {fieldName}");
                    
                    case JsonEvent.ValueNull:
                        return Error($"Found null for a required field: {fieldName}");
                    
                    case JsonEvent.ArrayStart:
                        return Error($"Found array in array item. but expect: {typeId}, field: {fieldName}");
                    
                    case JsonEvent.ObjectStart:
                        if (typeId == TypeId.Complex || typeId == TypeId.Union) {
                            if (ValidateObject(ref parser, type))
                                continue;
                            return false;
                        }
                        return Error($"Found object but expect: {typeId}, field: {fieldName}");
                    
                    case JsonEvent.Error:
                        return Error(parser.error.msg.ToString());
                    
                    case JsonEvent.ObjectEnd:
                        if (!isArray)
                            return true;
                        throw new InvalidOperationException($"expect object end: {ev}");
                    case JsonEvent.ArrayEnd:
                        if (isArray)
                            return true;
                        throw new InvalidOperationException($"expect array end: {ev}");
                }
            }
        }
        
        private bool FindField (ValidationType type, ref Bytes key, out ValidationField field, out TypeId typeId) {
            foreach (var typeField in type.fields) {
                if (key.IsEqual(ref typeField.name)) {
                    field   = typeField;
                    typeId  = typeField.typeId;
                    return true;
                }
            }
            error = $"field not found in type: {type}, key: {key}";
            field = null;
            typeId = TypeId.None; 
            return false;
        }
        
        private bool FindUnion (ValidationUnion union, ref Bytes discriminant, out ValidationType type, out TypeId typeId) {
            var types = union.types;
            for (int n = 0; n < types.Length; n++) {
                if (discriminant.IsEqual(ref types[n].discriminant)) {
                    type    = types[n].type;
                    typeId  = types[n].type.typeId; // todo
                    return true;
                }
            }
            type    = null;
            typeId  = TypeId.None;
            return false;
        }
        
        private bool FindEnum (ValidationType type, ref Bytes value) {
            var enumValues = type.enumValues;
            for (int n = 0; n < enumValues.Length; n++) {
                if (enumValues[n].IsEqual(ref value))
                    return true;
            }
            error = $"enum value not found. value: {value}";
            return false;
        }
        
        private bool Error (string error) {
            this.error = error;
            return false;
        }

        public void Dispose() {
            jsonBytes.Dispose();
        }
    }
}