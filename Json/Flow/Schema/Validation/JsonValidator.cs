// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using Friflo.Json.Burst;

namespace Friflo.Json.Flow.Schema.Validation
{
    public class JsonValidator : IDisposable
    {
        private Bytes               jsonBytes = new Bytes(128);
        private string              error;
        
        public void Dispose() {
            jsonBytes.Dispose();
        }
        
        public bool Validate (ref JsonParser parser, string json, ValidationType type, out string error) {
            this.error = null;
            jsonBytes.Clear();
            jsonBytes.AppendString(json);
            parser.InitParser(jsonBytes);
            var ev = parser.NextEvent();
            if (ev == JsonEvent.ObjectStart) {
                if (ValidateObject(ref parser, type)) {
                    ev = parser.NextEvent();
                    if (ev == JsonEvent.EOF) {
                        error = null;
                        return true;
                    }
                    error = "Expected EOF in JSON value";
                    return false;
                }
                error = this.error;
                return false;
            }
            throw new InvalidOperationException("Currently JsonValidator support only JSON objects");
        }
        
        private bool ValidateObject (ref JsonParser parser, ValidationType type)
        {
            if (type.typeId == TypeId.Union) {
                var ev      = parser.NextEvent();
                var unionType = type.unionType;
                if (ev != JsonEvent.ValueString) {
                    return Error($"Expect discriminator string as first member. Expect: {unionType.discriminatorStr}, was: {ev}");
                }
                if (!parser.key.IsEqual(ref unionType.discriminator)) {
                    return Error($"Unexpected discriminator name. was: {parser.key}, expect: {unionType.discriminatorStr}");
                }
                if (!ValidationUnion.FindUnion(unionType, ref parser.value, out type)) {
                    return Error($"Unknown discriminant: {parser.value}");
                }
            }
            while (true) {
                var             ev = parser.NextEvent();
                ValidationField field;
                string          msg;
                switch (ev) {
                    case JsonEvent.ValueString:
                        if (!ValidationType.FindField(type, ref parser.key, out field, out msg))
                            return Error(msg);
                        if (ValidateString (ref parser.value, field.type, out msg))
                            continue;
                        return Error($"{msg}, field: {field}");
                        
                    case JsonEvent.ValueNumber:
                        if (!ValidationType.FindField(type, ref parser.key, out field, out msg))
                            return Error(msg);
                        if (ValidateNumber(ref parser.value, type, out msg))
                            continue;
                        return Error($"{msg}, field: {field}");
                        
                    case JsonEvent.ValueBool:
                        if (!ValidationType.FindField(type, ref parser.key, out field, out msg))
                            return Error(msg);
                        if (field.typeId == TypeId.Boolean)
                            continue;
                        return Error($"Found boolean but expect: {field.typeId}, field: {field}");
                    
                    case JsonEvent.ValueNull:
                        if (!ValidationType.FindField(type, ref parser.key, out field, out msg))
                            return Error(msg);
                        if (!field.required)
                            continue;
                        return Error($"Found null for a required field: {field}");
                    
                    case JsonEvent.ArrayStart:
                        if (!ValidationType.FindField(type, ref parser.key, out field, out msg))
                            return Error(msg);
                        if (field.isArray) {
                            if (ValidateElement (ref parser, field.type, field.fieldName, true))
                                continue;
                            return false;
                        }
                        return Error($"Found array but expect: {field.typeId}, field: {field}");
                    
                    case JsonEvent.ObjectStart:
                        if (!ValidationType.FindField(type, ref parser.key, out field, out msg))
                            return Error(msg);
                        if (field.typeId == TypeId.Complex) {
                            if (field.isDictionary) {
                                if (ValidateElement (ref parser, field.type, field.fieldName, false))
                                    continue;
                                return false;
                            }
                            if (ValidateElement (ref parser, field.type, field.fieldName, true))
                                continue;
                            return false;
                        }
                        return Error($"Found object but expect: {field.typeId}, field: {field}");
                    
                    case JsonEvent.ObjectEnd:
                        return true;
                    
                    case JsonEvent.ArrayEnd:
                        return Error($"Found array end in object: {ev}");
                    
                    case JsonEvent.Error:
                        return Error(parser.error.msg.ToString());

                    default:
                        return Error($"Unexpected JSON event in object: {ev}");
                }
            }
        }
        
        private bool ValidateElement (ref JsonParser parser, ValidationType type, string fieldName, bool isArray) {
            while (true) {
                var     ev = parser.NextEvent();
                string  msg;
                switch (ev) {
                    case JsonEvent.ValueString:
                        if (ValidateString(ref parser.value, type, out msg))
                            continue;
                        return Error($"{msg}, field: {fieldName}");
                        
                    case JsonEvent.ValueNumber:
                        if (ValidateNumber(ref parser.value, type, out msg))
                            continue;
                        return Error($"{msg}, field: {fieldName}");
                        
                    case JsonEvent.ValueBool:
                        if (type.typeId == TypeId.Boolean)
                            continue;
                        return Error($"Found boolean but expect: {type.typeId}, field: {fieldName}");
                    
                    case JsonEvent.ValueNull:
                        return Error($"Found null for a required field: {fieldName}");
                    
                    case JsonEvent.ArrayStart:
                        return Error($"Found array in array item. but expect: {type.typeId}, field: {fieldName}");
                    
                    case JsonEvent.ObjectStart:
                        if (type.typeId == TypeId.Complex || type.typeId == TypeId.Union) {
                            // in case of a dictionary the key is not relevant
                            if (ValidateObject(ref parser, type))
                                continue;
                            return false;
                        }
                        return Error($"Found object but expect: {type.typeId}, field: {fieldName}");
                    
                    case JsonEvent.ObjectEnd:
                        if (!isArray)
                            return true;
                        return Error($"Found object end in array: {ev}");
                    
                    case JsonEvent.ArrayEnd:
                        if (isArray)
                            return true;
                        return Error($"Found array end in object: {ev}");
                    
                    case JsonEvent.Error:
                        return Error(parser.error.msg.ToString());

                    default:
                        return Error($"Unexpected JSON event: {ev}");
                }
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Error (string error) {
            this.error = error;
            return false;
        }
        
        // --- static helper
        // => using static prevent over writing previous error messages
        private static bool ValidateString (ref Bytes value, ValidationType type, out string msg) {
            var typeId = type.typeId;
            switch (typeId) {
                case TypeId.String:
                case TypeId.BigInteger:
                case TypeId.DateTime:
                    msg = null;
                    return true;
                case TypeId.Enum:
                    return ValidationType.FindEnum(type, ref value, out msg);
                default:
                    msg = $"Found string but expect: {typeId}";
                    return false;
            }
        }
        
        private static bool ValidateNumber (ref Bytes value, ValidationType type, out string msg) {
            switch (type.typeId) {
                case TypeId.Uint8:
                case TypeId.Int16:
                case TypeId.Int32:
                case TypeId.Int64:
                case TypeId.Float:
                case TypeId.Double:
                    msg = null;
                    return true;
                default:
                    msg = $"Found number but expect: {type.typeId}";
                    return false;
            }
        }
    }
}