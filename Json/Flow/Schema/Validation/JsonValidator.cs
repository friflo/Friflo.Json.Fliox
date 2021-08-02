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
            while (true) {
                var ev      = parser.NextEvent();
                ValidationField field;
                TypeId          typeId;
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
                        if (field.isArray)
                            continue;
                        return Error($"Found array but expect: {typeId}, field: {field}");
                    
                    case JsonEvent.ObjectStart:
                        if (!FindField(type, ref parser.key, out field, out typeId))
                            return false;
                        if (typeId == TypeId.Complex) {
                            if (ValidateObject(ref parser, field.type))
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
        
        private bool Error (string error) {
            this.error = error;
            return false;
        }

        public void Dispose() {
            jsonBytes.Dispose();
        }
    }
}