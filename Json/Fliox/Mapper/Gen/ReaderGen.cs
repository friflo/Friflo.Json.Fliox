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

        public bool Read (string name, PropField field, ref int value) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen(field.fieldType, ref value);
            value = parser.ValueAsByte(out bool success);
            return success;
        }
        
        public bool Read<T> (string name, PropField field, ref T value) {
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
    }
}