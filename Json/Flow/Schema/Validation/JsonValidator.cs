// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Friflo.Json.Burst;

namespace Friflo.Json.Flow.Schema.Validation
{
    public class JsonValidator : IDisposable
    {
        private  Bytes                          jsonBytes = new Bytes(128);
        private             string              errorMsg;
        private  readonly   List<List<bool>>    foundFieldsCache = new List<List<bool>>();
        
        public void Dispose() {
            jsonBytes.Dispose();
        }
        
        private void Init(ref JsonParser parser, string json) {
            errorMsg = null;
            jsonBytes.Clear();
            jsonBytes.AppendString(json);
            parser.InitParser(jsonBytes);
        }
        
        private bool Return(ref JsonParser parser, bool success, out string error) {
            if (!success) {
                error = errorMsg;
                return false;
            }
            var ev = parser.NextEvent();
            if (ev == JsonEvent.EOF) {
                error = null;
                return true;
            }
            error = "Expected EOF in JSON value";
            return false;
        }

        public bool ValidateObject (ref JsonParser parser, string json, ValidationType type, out string error) {
            Init(ref parser, json);
            var ev = parser.NextEvent();
            if (ev == JsonEvent.ObjectStart) {
                bool success = ValidateObject(ref parser, type, 0);
                return Return(ref parser, success, out error);    
            }
            error = $"ValidateObject expect object. was: {ev}";
            return false;
        }
        
        public bool ValidateObjectMap (ref JsonParser parser, string json, ValidationType type, out string error) {
            Init(ref parser, json);
            var ev = parser.NextEvent();
            if (ev == JsonEvent.ObjectStart) {
                bool success = ValidateElement(ref parser, type, "", false, 0);
                return Return(ref parser, success, out error);    
            }
            error = $"ValidateObjectMap expect object. was: {ev}";
            return false;
        }

        public bool ValidateArray (ref JsonParser parser, string json, ValidationType type, out string error) {
            Init(ref parser, json);
            var ev = parser.NextEvent();
            if (ev == JsonEvent.ArrayStart) {
                bool success = ValidateElement(ref parser, type, "", true, 0);
                return Return(ref parser, success, out error);    
            }
            error = $"ValidateArray expect array. was: {ev}";
            return false;
        }
        
        private bool ValidateObject (ref JsonParser parser, ValidationType type, int depth)
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
            // clear foundFields List
            while (foundFieldsCache.Count <= depth) {
                foundFieldsCache.Add(new List<bool>()); 
            }
            List<bool> foundFields = foundFieldsCache[depth];
            foundFields.Clear();
            for (int n= 0; n < type.requiredFieldsCount; n++) {
                foundFields.Add(false);
            }
            while (true) {
                var             ev = parser.NextEvent();
                ValidationField field;
                string          msg;
                switch (ev) {
                    case JsonEvent.ValueString:
                        if (!ValidationType.FindField(type, ref parser.key, out field, out msg, foundFields))
                            return Error(msg);
                        if (ValidateString (ref parser.value, field.type, out msg))
                            continue;
                        return Error($"{msg}, field: {field}");
                        
                    case JsonEvent.ValueNumber:
                        if (!ValidationType.FindField(type, ref parser.key, out field, out msg, foundFields))
                            return Error(msg);
                        if (ValidateNumber(ref parser.value, type, out msg))
                            continue;
                        return Error($"{msg}, field: {field}");
                        
                    case JsonEvent.ValueBool:
                        if (!ValidationType.FindField(type, ref parser.key, out field, out msg, foundFields))
                            return Error(msg);
                        if (field.typeId == TypeId.Boolean)
                            continue;
                        return Error($"Found boolean but expect: {field.typeId}, field: {field}");
                    
                    case JsonEvent.ValueNull:
                        if (!ValidationType.FindField(type, ref parser.key, out field, out msg, foundFields))
                            return Error(msg);
                        if (!field.required)
                            continue;
                        return Error($"Found null for a required field: {field}");
                    
                    case JsonEvent.ArrayStart:
                        if (!ValidationType.FindField(type, ref parser.key, out field, out msg, foundFields))
                            return Error(msg);
                        if (field.isArray) {
                            if (ValidateElement (ref parser, field.type, field.fieldName, true, depth))
                                continue;
                            return false;
                        }
                        return Error($"Found array but expect: {field.typeId}, field: {field}");
                    
                    case JsonEvent.ObjectStart:
                        if (!ValidationType.FindField(type, ref parser.key, out field, out msg, foundFields))
                            return Error(msg);
                        if (field.typeId == TypeId.Complex) {
                            if (field.isDictionary) {
                                if (ValidateElement (ref parser, field.type, field.fieldName, false, depth))
                                    continue;
                                return false;
                            }
                            if (ValidateElement (ref parser, field.type, field.fieldName, true, depth))
                                continue;
                            return false;
                        }
                        return Error($"Found object but expect: {field.typeId}, field: {field}");
                    
                    case JsonEvent.ObjectEnd:
                        var foundCount = 0;
                        foreach (var found in foundFields) {
                            if (found)
                                foundCount++;
                        }
                        if (foundCount < type.requiredFieldsCount) {
                            // return Error($"missing required fields in type: {type}, missing: {type.requiredFieldsCount - foundCount}");
                        }
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
        
        private bool ValidateElement (ref JsonParser parser, ValidationType type, string fieldName, bool isArray, int depth) {
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
                            if (ValidateObject(ref parser, type, depth + 1))
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
        private bool Error (string msg) {
            if (errorMsg != null)
                throw new InvalidOperationException($"error already set. Error: {errorMsg}");
            errorMsg = msg;
            return false;
        }
        
        // --- static helper
        // => using static prevent over writing previous error messages
        private static bool ValidateString (ref Bytes value, ValidationType type, out string msg) {
            switch (type.typeId) {
                case TypeId.String:
                case TypeId.BigInteger:
                case TypeId.DateTime:
                    msg = null;
                    return true;
                case TypeId.Enum:
                    return ValidationType.FindEnum(type, ref value, out msg);
                default:
                    msg = $"Found string but expect: {type.typeId}";
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