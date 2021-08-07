// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Friflo.Json.Burst;

namespace Friflo.Json.Flow.Schema.Validation
{
    public class JsonValidator : IDisposable
    {
        internal            JsonParser      parser; // on top enabling instance offset 0
        private             Bytes           jsonBytes = new Bytes(128);
        private             ValidationError validationError;
        private  readonly   List<bool[]>    foundFieldsCache = new List<bool[]>();
        private  readonly   StringBuilder   sb = new StringBuilder();
        private  readonly   List<string>    missingFields = new List<string>();
        private  readonly   Regex           dateTime;
        private  readonly   Regex           bigInt;
        
        
        // RFC 3339 + milliseconds
        private  static readonly Regex  DateTime    = new Regex(@"\b^[1-9]\d{3}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d{3}Z$\b",   RegexOptions.Compiled);
        private  static readonly Regex  BigInt      = new Regex(@"\b^-?[0-9]+$\b",                                          RegexOptions.Compiled);
        
        public              bool            qualifiedTypeErrors;
        
        public JsonValidator (bool qualifiedTypeErrors = false) {
            this.qualifiedTypeErrors    = qualifiedTypeErrors;
            dateTime                    = DateTime;
            bigInt                      = BigInt;
        }
        
        public void Dispose() {
            parser.Dispose();
            jsonBytes.Dispose();
            foundFieldsCache.Clear();
            sb.Clear();
            missingFields.Clear();
        }
        
        private void Init(string json) {
            validationError = new ValidationError();
            jsonBytes.Clear();
            jsonBytes.AppendString(json);
            parser.InitParser(jsonBytes);
        }
        
        private bool Return(ValidationType type, bool success, out string error) {
            if (!success) {
                error = validationError.AsString(sb, qualifiedTypeErrors);
                return false;
            }
            var ev = parser.NextEvent();
            if (ev == JsonEvent.EOF) {
                error = null;
                return true;
            }
            return RootError(type, "Expected EOF after reading JSON", out error);
        }

        public bool ValidateObject (string json, ValidationType type, out string error) {
            Init(json);
            var ev = parser.NextEvent();
            if (ev == JsonEvent.ObjectStart) {
                bool success = ValidateObject(type, 0);
                return Return(type, success, out error);    
            }
            return RootError(type, $"ValidateObject() expect object. was: {ev}", out error);
        }
        
        public bool ValidateObjectMap (string json, ValidationType type, out string error) {
            Init(json);
            var ev = parser.NextEvent();
            if (ev == JsonEvent.ObjectStart) {
                bool success = ValidateElement(type, null, 0);
                return Return(type, success, out error);    
            }
            return RootError(type, $"ValidateObjectMap() expect object. was: {ev}", out error);
        }

        public bool ValidateArray (string json, ValidationType type, out string error) {
            Init(json);
            var ev = parser.NextEvent();
            if (ev == JsonEvent.ArrayStart) {
                bool success = ValidateElement(type, null, 0);
                return Return(type, success, out error);    
            }
            return RootError(type, $"ValidateArray() expect array. was: {ev}", out error);
        }
        
        private bool ValidateObject (ValidationType type, int depth)
        {
            if (type.typeId == TypeId.Union) {
                var ev      = parser.NextEvent();
                var unionType = type.unionType;
                if (ev != JsonEvent.ValueString) {
                    return ErrorType("Expect discriminator as first member.", ev.ToString(), unionType.discriminatorStr, null, type);
                }
                if (!parser.key.IsEqual(ref unionType.discriminator)) {
                    return ErrorType("Invalid discriminator.", $"'{parser.key}'", unionType.discriminatorStr, null, type);
                }
                if (!ValidationUnion.FindUnion(unionType, ref parser.value, out var newType)) {
                    var expect = unionType.GetTypesAsString();
                    return ErrorType("Invalid discriminant.", $"'{parser.value}'", expect, null, type);
                }
                type = newType;
            }
            var foundFields = GetFoundFields(type, foundFieldsCache, depth);

            while (true) {
                var             ev = parser.NextEvent();
                ValidationField field;
                switch (ev) {
                    case JsonEvent.ValueString:
                        if (!ValidationType.FindField(type, this, out field, foundFields))
                            return false;
                        if (ValidateString (ref parser.value, field.type, type))
                            continue;
                        return false;
                        
                    case JsonEvent.ValueNumber:
                        if (!ValidationType.FindField(type, this, out field, foundFields))
                            return false;
                        if (ValidateNumber(field.type, type))
                            continue;
                        return false;
                        
                    case JsonEvent.ValueBool:
                        if (!ValidationType.FindField(type, this, out field, foundFields))
                            return false;
                        if (field.typeId == TypeId.Boolean)
                            continue;
                        var value = parser.boolValue ? "true" : "false";
                        return ErrorType("Incorrect type.", value, field.typeName, field.type.@namespace, type);
                    
                    case JsonEvent.ValueNull:
                        if (!ValidationType.FindField(type, this, out field, foundFields))
                            return false;
                        if (!field.required)
                            continue;
                        return Error("Required property must not be null.", type);
                    
                    case JsonEvent.ArrayStart:
                        if (!ValidationType.FindField(type, this, out field, foundFields))
                            return false;
                        if (field.isArray) {
                            if (ValidateElement (field.type, type, depth))
                                continue;
                            return false;
                        }
                        return ErrorType("Incorrect type.", "array", field.typeName, field.type.@namespace, type);
                    
                    case JsonEvent.ObjectStart:
                        if (!ValidationType.FindField(type, this, out field, foundFields))
                            return false;
                        if (field.typeId == TypeId.Class) {
                            if (field.isDictionary) {
                                if (ValidateElement (field.type, type, depth))
                                    continue;
                                return false;
                            }
                            if (ValidateObject (field.type, depth + 1))
                                continue;
                            return false;
                        }
                        return ErrorType("Incorrect type.", "object", field.typeName, field.type.@namespace, type);
                    
                    case JsonEvent.ObjectEnd:
                        if (type.HasMissingFields(foundFields, missingFields)) {
                            var missing = string.Join(", ", missingFields);
                            return Error($"Missing required fields: [{missing}]", type);
                        }
                        return true;
                    
                    case JsonEvent.Error:
                        return Error(parser.error.GetMessageBody(), type);

                    default:
                        return Error($"Unexpected JSON event in object: {ev}", type);
                }
            }
        }
        
        private bool ValidateElement (ValidationType type, ValidationType parent, int depth) {
            while (true) {
                var     ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                        if (ValidateString(ref parser.value, type, parent))
                            continue;
                        return false;
                        
                    case JsonEvent.ValueNumber:
                        if (ValidateNumber(type, parent))
                            continue;
                        return false;
                        
                    case JsonEvent.ValueBool:
                        if (type.typeId == TypeId.Boolean)
                            continue;
                        var value = parser.boolValue ? "true" : "false";
                        return ErrorType("Incorrect type.", value, type.name, null, parent);
                    
                    case JsonEvent.ValueNull:
                        return Error("Element must not be null.", parent);
                    
                    case JsonEvent.ArrayStart:
                        var expect = ValidationType.GetName(type, qualifiedTypeErrors);
                        return Error($"Found array as array item. expect: {expect}", parent); // todo
                    
                    case JsonEvent.ObjectStart:
                        if (type.typeId == TypeId.Class || type.typeId == TypeId.Union) {
                            // in case of a dictionary the key is not relevant
                            if (ValidateObject(type, depth + 1))
                                continue;
                            return false;
                        }
                        return ErrorType("Incorrect type.", "object", type.name, type.@namespace, parent);
                    
                    case JsonEvent.ObjectEnd:
                        return true;

                    case JsonEvent.ArrayEnd:
                        return true;
                    
                    case JsonEvent.Error:
                        return Error(parser.error.GetMessageBody(), parent);

                    default:
                        return Error($"Unexpected JSON event: {ev}", parent);
                }
            }
        }
        
        private bool RootError (ValidationType type, string msg, out string error) {
            if (parser.Event == JsonEvent.Error) {
                Error(parser.error.GetMessageBody(), type);
            } else {
                Error(msg, type);
            }
            error = validationError.AsString(sb, qualifiedTypeErrors);
            return false;
        }
        
        internal bool ErrorType (string msg, string was, string expect, string expectNamespace, ValidationType type) {
            if (validationError.msg != null) {
                throw new InvalidOperationException($"error already set. Error: {validationError}");
            }
            validationError = new ValidationError(msg, was, expect, expectNamespace, type, parser.GetPath(), parser.Position);
            return false;         
        }

        internal bool Error(string msg, ValidationType type) {
            if (validationError.msg != null) {
                throw new InvalidOperationException($"error already set. Error: {validationError}");
            }
            validationError = new ValidationError(msg, type, parser.GetPath(), parser.Position);
            return false;
        }
        
        // --- helper methods
        private bool ValidateString (ref Bytes value, ValidationType type, ValidationType parent) {
            switch (type.typeId) {
                case TypeId.String:
                    return true;
                
                case TypeId.BigInteger:
                    var str = value.ToString();
                    if (bigInt.IsMatch(str)) {
                        return true;
                    }
                    return Error($"Invalid BigInteger: '{str}'", parent);
                
                case TypeId.DateTime:
                    str = value.ToString();
                    if (dateTime.IsMatch(str)) {
                        return true;
                    }
                    return Error($"Invalid DateTime: '{str}'", parent);
                
                case TypeId.Enum:
                    return ValidationType.FindEnum(type, ref value, this, parent);
                
                default:
                    return ErrorType("Incorrect type.", $"'{Truncate(ref value)}'", type.name, type.@namespace, parent);
            }
        }
        
        private static string Truncate (ref Bytes value) {
            var str = value.ToString();
            if (str.Length < 20)
                return str;
            return str.Substring(20) + "...";
        }
        
        private bool ValidateNumber (ValidationType type, ValidationType owner) {
            var typeId = type.typeId; 
            switch (typeId) {
                case TypeId.Uint8:
                case TypeId.Int16:
                case TypeId.Int32:
                case TypeId.Int64:
                    if (parser.isFloat) {
                        return ErrorType("Invalid integer.", parser.value.ToString(), type.name, type.@namespace, owner);
                    }
                    var value = parser.ValueAsLong(out bool success);
                    if (!success) {
                        return ErrorType("Invalid integer.", parser.value.ToString(), type.name, type.@namespace, owner);
                    }
                    switch (typeId) {
                        case TypeId.Uint8: if (          0 <= value && value <=        255) { return true; } break;   
                        case TypeId.Int16: if (     -32768 <= value && value <=      32767) { return true; } break;
                        case TypeId.Int32: if (-2147483648 <= value && value <= 2147483647) { return true; } break;
                        case TypeId.Int64:                                                  { return true; }
                        default:
                            throw new InvalidOperationException("cant be reached");
                    }
                    return ErrorType("Integer out of range.", parser.value.ToString(), type.name, type.@namespace, owner);
                
                case TypeId.Float:
                case TypeId.Double:
                    return true;
                default:
                    return ErrorType("Incorrect type.", parser.value.ToString(), type.name, type.@namespace, owner);
            }
        }
        
        private static bool[] GetFoundFields(ValidationType type, List<bool[]> foundFieldsCache, int depth) {
            while (foundFieldsCache.Count <= depth) {
                foundFieldsCache.Add(null);
            }
            int requiredCount = type.requiredFieldsCount;
            bool[] foundFields = foundFieldsCache[depth];
            if (foundFields == null || foundFields.Length < requiredCount) {
                foundFields = foundFieldsCache[depth] = new bool[requiredCount];
            }
            for (int n= 0; n < requiredCount; n++) {
                foundFields[n] = false;
            }
            return foundFields;
        }
    }
}