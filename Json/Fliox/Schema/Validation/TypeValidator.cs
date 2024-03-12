// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Friflo.Json.Burst;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Friflo.Json.Fliox.Schema.Validation
{
    public sealed class TypeValidator : IDisposable
    {
        internal            Utf8JsonParser  parser; // on top enabling instance offset 0
        private             ValidationError validationError;
        private  readonly   List<bool[]>    foundFieldsCache = new List<bool[]>();
        private  readonly   StringBuilder   sb = new StringBuilder();
        private  readonly   Regex           dateTime;
        private  readonly   Regex           bigInt;
        private  readonly   Regex           guid;
        
        public              bool            qualifiedTypeErrors;
        
        // RegEx Tester:    http://regexstorm.net/tester  (C# .NET)
        // ISO 8601:        https://en.wikipedia.org/wiki/ISO_8601

        /// <summary>ISO 8601 (RFC 3339) using optional fractions of a second. See <see cref="Bytes.DateTimeFormat"/></summary>
        private  static readonly Regex  DateTime    = new Regex(@"\b^[1-9]\d{3}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(.\d{1,6})?Z$\b",      RegexOptions.Compiled);
        private  static readonly Regex  BigInt      = new Regex(@"\b^-?[0-9]+$\b",                                                  RegexOptions.Compiled);
        private  static readonly Regex  Guid        = new Regex(@"\b^[{]?[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}[}]?$\b",RegexOptions.Compiled);

        public TypeValidator (bool qualifiedTypeErrors = false) {
            this.qualifiedTypeErrors    = qualifiedTypeErrors;
            dateTime                    = DateTime;
            bigInt                      = BigInt;
            guid                        = Guid;
        }
        
        public void Dispose() {
            parser.Dispose();
            foundFieldsCache.Clear();
            sb.Clear();
        }
        
        private void Init(in JsonValue json) {
            validationError = new ValidationError();
            parser.InitParser(json);
        }
        
        private bool Return(ValidationTypeDef typeDef, bool success, out string error) {
            if (!success) {
                error = validationError.AsString(sb, qualifiedTypeErrors);
                return false;
            }
            var ev = parser.NextEvent();
            if (ev == JsonEvent.EOF) {
                error = null;
                return true;
            }
            return RootError(typeDef, "Expected EOF after reading JSON", out error);
        }
        
        public bool ValidateJson(in JsonValue json, out string error) {
            parser.InitParser(json);
            parser.SkipTree();
            var success = !parser.error.ErrSet;
            if (success) {
                error = null;
            } else {
                error = parser.error.msg.AsString();
            }
            return success;
        }
        
        public bool Validate (in JsonValue json, ValidationType type, out string error) {
            Init(json);
            var ev      = parser.NextEvent();
            var typeDef = type.typeDef;
            switch (ev) {
                case JsonEvent.ValueNull:
                    if (type.required) {
                        ErrorType("Incorrect type.", "null", false, typeDef.name, typeDef.@namespace, null);
                        return Return(typeDef, false, out error);
                    }
                    return Return(typeDef, true, out error);
                case JsonEvent.ValueBool: {
                    var success = ValidateBoolean(typeDef, null);
                    return Return(typeDef, success, out error);
                }
                case JsonEvent.ValueNumber: {
                    var success = ValidateNumber(typeDef, null);
                    return Return(typeDef, success, out error);
                }
                case JsonEvent.ValueString:{
                    var success = ValidateString(parser.value, typeDef, null);
                    return Return(typeDef, success, out error);
                }
                case JsonEvent.ObjectStart: {
                    bool success = ValidateObjectIntern(typeDef, 0);
                    return Return(typeDef, success, out error);
                }
                case JsonEvent.ArrayStart: {
                    if (!type.isArray) {
                        ErrorType("Incorrect type.", "array", false, typeDef.name, typeDef.@namespace, null);
                        return Return(typeDef, false, out error);
                    }
                    bool success = ValidateElement(typeDef, type.isNullableElement, null, 0);
                    return Return(typeDef, success, out error);
                }
                case JsonEvent.Error:
                    error = parser.error.GetMessageBody();
                    return false;
            }
            throw new InvalidOperationException($"Unexpected JSON event: {ev}");
        }
        
        public bool ValidateObject (in JsonValue json, ValidationType type, out string error) {
            Init(json);
            var ev = parser.NextEvent();
            var typeDef = type.typeDef;
            if (ev == JsonEvent.ObjectStart) {
                bool success = ValidateObjectIntern(typeDef, 0);
                return Return(typeDef, success, out error);
            }
            return RootError(typeDef, "expect object. was:", out error);
        }
        
        public bool ValidateObjectMap (in JsonValue json, ValidationType type, out string error) {
            Init(json);
            var ev      = parser.NextEvent();
            var typeDef = type.typeDef;
            if (ev == JsonEvent.ObjectStart) {
                bool success = ValidateElement(typeDef, false, null, 0);
                return Return(typeDef, success, out error);    
            }
            return RootError(typeDef, "expect object. was:", out error);
        }
        
        public bool ValidateArray (in JsonValue json, ValidationType type, out string error) {
            Init(json);
            var ev      = parser.NextEvent();
            var typeDef = type.typeDef;
            if (ev == JsonEvent.ArrayStart) {
                bool success = ValidateElement(typeDef, false, null, 0);
                return Return(typeDef, success, out error);    
            }
            return RootError(typeDef, "expect array. was:", out error);
        }
        
        private bool ValidateObjectIntern (ValidationTypeDef typeDef, int depth)
        {
            if (typeDef.typeId == TypeId.JsonValue) {
                return parser.SkipTree();
            }
            if (typeDef.typeId == TypeId.Union) {
                var ev      = parser.NextEvent();
                var unionType = typeDef.unionType;
                if (ev != JsonEvent.ValueString) {
                    return ErrorType("Expect discriminator as first member.", ev.ToString(), false, unionType.discriminatorStr, null, typeDef);
                }
                if (!unionType.discriminator.IsEqual(parser.key)) {
                    return ErrorType("Invalid discriminator.", parser.key.AsString(), true, unionType.discriminatorStr, null, typeDef);
                }
                if (!ValidationUnion.FindUnion(unionType, parser.value, out var newType)) {
                    var expect = unionType.TypesAsString;
                    return ErrorType("Invalid discriminant.", parser.value.AsString(), true, expect, null, typeDef);
                }
                typeDef = newType;
            }
            if (typeDef.typeId != TypeId.Class) {
                return ErrorType("Incorrect type.", "object", false, typeDef.name, typeDef.@namespace, typeDef);
            }
            var foundFields = GetFoundFields(typeDef, foundFieldsCache, depth);

            while (true) {
                var             ev = parser.NextEvent();
                ValidationType fieldType;
                switch (ev) {
                    case JsonEvent.ValueString:
                        if (!ValidationTypeDef.FindField(typeDef, this, out fieldType, foundFields))
                            return false;
                        if (ValidateString (parser.value, fieldType.typeDef, typeDef))
                            continue;
                        return false;
                        
                    case JsonEvent.ValueNumber:
                        if (!ValidationTypeDef.FindField(typeDef, this, out fieldType, foundFields))
                            return false;
                        if (ValidateNumber(fieldType.typeDef, typeDef))
                            continue;
                        return false;
                        
                    case JsonEvent.ValueBool:
                        if (!ValidationTypeDef.FindField(typeDef, this, out fieldType, foundFields))
                            return false;
                        if (ValidateBoolean(fieldType.typeDef, typeDef))
                            continue;
                        return false;
                    
                    case JsonEvent.ValueNull:
                        if (!ValidationTypeDef.FindField(typeDef, this, out fieldType, foundFields))
                            return false;
                        if (!fieldType.required)
                            continue;
                        return Error("Required property must not be null.", typeDef);
                    
                    case JsonEvent.ArrayStart:
                        if (!ValidationTypeDef.FindField(typeDef, this, out fieldType, foundFields))
                            return false;
                        if (fieldType.isArray) {
                            if (ValidateElement (fieldType.typeDef, fieldType.isNullableElement, typeDef, depth))
                                continue;
                            return false;
                        }
                        if (fieldType.typeId == TypeId.JsonValue) {
                            if (parser.SkipTree())
                                continue;
                            return false;
                        }
                        return ErrorType("Incorrect type.", "array", false, fieldType.typeName, fieldType.typeDef.@namespace, typeDef);
                    
                    case JsonEvent.ObjectStart:
                        if (!ValidationTypeDef.FindField(typeDef, this, out fieldType, foundFields))
                            return false;
                        if (fieldType.isDictionary) {
                            if (ValidateElement (fieldType.typeDef, fieldType.isNullableElement, typeDef, depth))
                                continue;
                            return false;
                        }
                        if (IsObjectType(fieldType.typeId)) {
                            if (ValidateObjectIntern (fieldType.typeDef, depth + 1))
                                continue;
                            return false;
                        }
                        return ErrorType("Incorrect type.", "object", false, fieldType.typeName, fieldType.typeDef.@namespace, typeDef);
                    
                    case JsonEvent.ObjectEnd:
                        if (typeDef.HasMissingFields(foundFields, sb)) {
                            return ErrorValue("Missing required fields:", sb.ToString(), false, typeDef);
                        }
                        return true;
                    
                    case JsonEvent.Error:
                        return Error(parser.error.GetMessageBody(), typeDef);

                    default:
                        return Error($"Unexpected JSON event in object: {ev}", typeDef);
                }
            }
        }
        
        private bool ValidateElement (ValidationTypeDef typeDef, bool isNullableElement, ValidationTypeDef parent, int depth) {
            while (true) {
                var     ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                        if (ValidateString(parser.value, typeDef, parent))
                            continue;
                        return false;
                        
                    case JsonEvent.ValueNumber:
                        if (ValidateNumber(typeDef, parent))
                            continue;
                        return false;
                        
                    case JsonEvent.ValueBool:
                        if (ValidateBoolean(typeDef, parent))
                            continue;
                        return false;
                    
                    case JsonEvent.ValueNull:
                        if (isNullableElement)
                            continue;
                        return Error("Element must not be null.", parent);
                    
                    case JsonEvent.ArrayStart:
                        if (typeDef.typeId == TypeId.JsonValue) {
                            if (parser.SkipTree())
                                continue;
                            return false;
                        }
                        var expect = ValidationTypeDef.GetName(typeDef, qualifiedTypeErrors);
                        return Error($"Found array as array item. expect: {expect}", parent); // todo
                    
                    case JsonEvent.ObjectStart:
                        if (IsObjectType(typeDef.typeId)) {
                            // in case of a dictionary the key is not relevant
                            if (ValidateObjectIntern(typeDef, depth + 1))
                                continue;
                            return false;
                        }
                        return ErrorType("Incorrect type.", "object", false, typeDef.name, typeDef.@namespace, parent);
                    
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
        
        private bool RootError (ValidationTypeDef typeDef, string msg, out string error) {
            if (parser.Event == JsonEvent.Error) {
                Error(parser.error.GetMessageBody(), typeDef);
            } else {
                string errorValue = GetErrorValue();
                ErrorValue(msg, errorValue, false, typeDef);
            }
            error = validationError.AsString(sb, qualifiedTypeErrors);
            return false;
        }
        
        private string GetErrorValue () {
            var ev = parser.Event;
            switch (ev) {
                case JsonEvent.ValueNull:   return "null";
                case JsonEvent.ValueBool:   return parser.boolValue ? "true" : "false"; 
                case JsonEvent.ValueNumber:
                case JsonEvent.ValueString: return parser.value.ToString();
                case JsonEvent.ObjectStart: return "object";
                case JsonEvent.ArrayStart:  return "array";
                default:                    return ev.ToString();
            }
        }
        
        internal bool ErrorType (string msg, string was, bool isString, string expect, string expectNamespace, ValidationTypeDef typeDef) {
            if (validationError.msg != null) {
                throw new InvalidOperationException($"error already set. Error: {validationError}");
            }
            validationError = new ValidationError(msg, was, isString, expect, expectNamespace, typeDef, parser.GetPath(), parser.Position);
            return false;         
        }
        
        private bool Error(string msg, ValidationTypeDef typeDef) {
            if (validationError.msg != null) {
                throw new InvalidOperationException($"error already set. Error: {validationError}");
            }
            validationError = new ValidationError(msg, null, false, typeDef, parser.GetPath(), parser.Position);
            return false;
        }

        internal bool ErrorValue(string msg, string value, bool isString, ValidationTypeDef typeDef) {
            if (validationError.msg != null) {
                throw new InvalidOperationException($"error already set. Error: {validationError}");
            }
            validationError = new ValidationError(msg, value, isString, typeDef, parser.GetPath(), parser.Position);
            return false;
        }
        
        // --- helper methods
        private bool ValidateString (in Bytes value, ValidationTypeDef typeDef, ValidationTypeDef parent) {
            switch (typeDef.typeId) {
                case TypeId.String:
                case TypeId.JsonValue:
                case TypeId.JsonKey:
                    return true;
                
                case TypeId.BigInteger:
                    var str = value.AsString();
                    if (bigInt.IsMatch(str)) {
                        return true;
                    }
                    return ErrorValue("Invalid BigInteger:", str, true, parent);
                
                case TypeId.DateTime:
                    str = value.AsString();
                    if (dateTime.IsMatch(str)) {
                        return true;
                    }
                    return ErrorValue("Invalid DateTime:", str, true, parent);
                
                case TypeId.Guid:
                    str = value.AsString();
                    if (guid.IsMatch(str)) {
                        return true;
                    }
                    return ErrorValue("Invalid Guid:", str, true, parent);
                
                case TypeId.Enum:
                    return ValidationTypeDef.FindEnum(typeDef, value, this, parent);
                
                default:
                    return ErrorType("Incorrect type.", Truncate(value), true, typeDef.name, typeDef.@namespace, parent);
            }
        }
        
        private static string Truncate (in Bytes value) {
            var str = value.AsString();
            if (str.Length < 20)
                return str;
            return str.Substring(20) + "...";
        }
        
        private bool ValidateBoolean (ValidationTypeDef typeDef, ValidationTypeDef owner) {
            switch (typeDef.typeId) {
                case TypeId.Boolean:
                case TypeId.JsonValue:
                    return true;
            }
            var value = parser.boolValue ? "true" : "false";
            return ErrorType("Incorrect type.", value, false, typeDef.name, typeDef.@namespace, owner);
        }
        
        private bool ValidateNumber (ValidationTypeDef typeDef, ValidationTypeDef owner) {
            var typeId = typeDef.typeId; 
            switch (typeId) {
                case TypeId.Uint8:
                case TypeId.Int16:
                case TypeId.Int32:
                case TypeId.Int64:
                    
                // NON_CLS
                case TypeId.Int8:
                case TypeId.UInt16:
                case TypeId.UInt32:
                case TypeId.UInt64:
                    
                case TypeId.JsonKey:
                    if (parser.isFloat) {
                        return ErrorType("Invalid integer.", parser.value.AsString(), false, typeDef.name, typeDef.@namespace, owner);
                    }
                    var value = parser.ValueAsLong(out bool success);
                    if (!success) {
                        return ErrorType("Invalid integer.", parser.value.AsString(), false, typeDef.name, typeDef.@namespace, owner);
                    }
                    switch (typeId) {
                        case TypeId.Uint8: if (          0 <= value && value <=        255) { return true; } break;   
                        case TypeId.Int16: if (     -32768 <= value && value <=      32767) { return true; } break;
                        case TypeId.Int32: if (-2147483648 <= value && value <= 2147483647) { return true; } break;
                        case TypeId.Int64:                                                  { return true; }
                        
                        // NON_CLS
                        case TypeId.Int8:   if (-128 <= value && value <=        127)       { return true; } break;   
                        case TypeId.UInt16: if (   0 <= value && value <=      65535)       { return true; } break;
                        case TypeId.UInt32: if (   0 <= value && value <= 4294967295)       { return true; } break;
                        case TypeId.UInt64:                                                 { return true; }
                        //
                        case TypeId.JsonKey:                                                { return true; }
                        default:
                            throw new InvalidOperationException("cant be reached");
                    }
                    return ErrorType("Integer out of range.", parser.value.AsString(), false, typeDef.name, typeDef.@namespace, owner);
                
                case TypeId.Float:
                case TypeId.Double:
                case TypeId.JsonValue:
                    return true;
                default:
                    return ErrorType("Incorrect type.", parser.value.AsString(), false, typeDef.name, typeDef.@namespace, owner);
            }
        }
        
        private static bool[] GetFoundFields(ValidationTypeDef typeDef, List<bool[]> foundFieldsCache, int depth) {
            while (foundFieldsCache.Count <= depth) {
                foundFieldsCache.Add(null);
            }
            int requiredCount = typeDef.requiredFieldsCount;
            bool[] foundFields = foundFieldsCache[depth];
            if (foundFields == null || foundFields.Length < requiredCount) {
                foundFields = foundFieldsCache[depth] = new bool[requiredCount];
            }
            for (int n= 0; n < requiredCount; n++) {
                foundFields[n] = false;
            }
            return foundFields;
        }
        
        private static bool IsObjectType (TypeId typeId) {
            return typeId == TypeId.Class || typeId == TypeId.Union || typeId == TypeId.JsonValue;    
        }
    }
}