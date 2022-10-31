// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Mapper.Map
{
    delegate bool ReadFieldDelegate<in T>(T obj, PropField field, ref Reader reader);

    partial struct Reader
    {
        private bool HandleEventGen<TVal>(TypeMapper mapper, ref TVal value) {
            switch (parser.Event) {
                case JsonEvent.ValueNull:
                    if (!mapper.isNullable) {
                        value = ErrorIncompatible<TVal>(mapper.DataTypeName(), mapper, out bool success);
                        return success;
                    }
                    value = default;
                    return true;
                case JsonEvent.Error:
                    const string msg2 = "requirement: error must be handled by owner. Add missing JsonEvent.Error case to its Mapper";
                    throw new InvalidOperationException(msg2);
                // return null;
                default: {
                    value = default;
                    ErrorIncompatible<TVal>(mapper.DataTypeName(), mapper, out _);
                    return false;
                }
            }
        }

        // used specific name to avoid using it accidentally with a non class / struct type  
        public bool ReadObj<T> (string name, PropField field, ref T value) {
            if (parser.Event != JsonEvent.ObjectStart) {
                return HandleEventGen(field.fieldType, ref value);
            }
            if (value == null) {
                value = (T)field.fieldType.CreateInstance();
            }
            var mapper = (TypeMapper<T>)field.fieldType;
            mapper.Read(ref this, value, out bool success);
            return success;
        }
        
        // ------------------------------------------- bool ---------------------------------------------
        public bool Read (string name, PropField field, ref bool value) {
            if (parser.Event != JsonEvent.ValueBool)
                return HandleEventGen(field.fieldType, ref value);
            value = parser.ValueAsBool(out bool success);
            return success;
        }
        
        // --- nullable
        public bool Read (string name, PropField field, ref bool? value) {
            if (parser.Event != JsonEvent.ValueBool)
                return HandleEventGen(field.fieldType, ref value);
            value = parser.ValueAsBool(out bool success);
            return success;
        }
        
        // ------------------------------------------- number ---------------------------------------------
        // --- integer
        public bool Read (string name, PropField field, ref byte value) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen(field.fieldType, ref value);
            value = parser.ValueAsByte(out bool success);
            return success;
        }
        
        public bool Read (string name, PropField field, ref short value) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen(field.fieldType, ref value);
            value = parser.ValueAsShort(out bool success);
            return success;
        }
        
        public bool Read (string name, PropField field, ref int value) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen(field.fieldType, ref value);
            value = parser.ValueAsInt(out bool success);
            return success;
        }
        
        public bool Read (string name, PropField field, ref long value) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen(field.fieldType, ref value);
            value = parser.ValueAsLong(out bool success);
            return success;
        }
        
        // --- floating point ---
        public bool Read (string name, PropField field, ref float value) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen(field.fieldType, ref value);
            value = parser.ValueAsFloat(out bool success);
            return success;
        }
        
        public bool Read (string name, PropField field, ref double value) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen(field.fieldType, ref value);
            value = parser.ValueAsDouble(out bool success);
            return success;
        }
        
        // --------------------------------- nullable number ------------------------------------------
        // --- integer
        public bool Read (string name, PropField field, ref byte? value) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen(field.fieldType, ref value);
            value = parser.ValueAsByte(out bool success);
            return success;
        }
        
        public bool Read (string name, PropField field, ref short? value) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen(field.fieldType, ref value);
            value = parser.ValueAsShort(out bool success);
            return success;
        }
        
        public bool Read (string name, PropField field, ref int? value) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen(field.fieldType, ref value);
            value = parser.ValueAsInt(out bool success);
            return success;
        }
        
        public bool Read (string name, PropField field, ref long? value) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen(field.fieldType, ref value);
            value = parser.ValueAsLong(out bool success);
            return success;
        }
        
        // --- floating point ---
        public bool Read (string name, PropField field, ref float? value) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen(field.fieldType, ref value);
            value = parser.ValueAsFloat(out bool success);
            return success;
        }
        
        public bool Read (string name, PropField field, ref double? value) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen(field.fieldType, ref value);
            value = parser.ValueAsDouble(out bool success);
            return success;
        }
        
        // ------------------------------------------- string ---------------------------------------------
        public bool Read (string name, PropField field, ref string value) {
            if (parser.Event != JsonEvent.ValueString)
                return HandleEventGen(field.fieldType, ref value);
            value = parser.value.GetString(ref charBuf);
            return true;
        }
        
        public bool Read (string name, PropField field, ref JsonKey value) {
            if (parser.Event != JsonEvent.ValueString)
                return HandleEventGen(field.fieldType, ref value);
            value = new JsonKey(ref parser.value, ref parser.valueParser);
            return true;
        }
        
        // ------------------------------------------- any ---------------------------------------------
        // ReSharper disable once RedundantAssignment
        public bool Read (string name, PropField field, ref JsonValue value) {
            var stub = jsonWriterStub;
            if (stub == null)
                jsonWriterStub = stub = new Utf8JsonWriterStub();
            
            ref var serializer = ref stub.jsonWriter;
            serializer.InitSerializer();
            serializer.WriteTree(ref parser);
            var json    = serializer.json.AsArray();
            value       = new JsonValue (json);
            return true;
        }
    }
}