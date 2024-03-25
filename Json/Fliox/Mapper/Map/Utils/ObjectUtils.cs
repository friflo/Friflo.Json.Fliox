// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Mapper.Map
{
    public partial struct Reader
    {
        public bool StartObject<T>(TypeMapper<T> mapper, out bool success) {
            var ev = parser.Event;
            switch (ev) {
                case JsonEvent.ValueNull:
                    if (mapper.isNullable) {
                        success = true;
                        return false;
                    }
                    ErrorIncompatible<T>(mapper.DataTypeName(), mapper, out success);
                    success = false;
                    return false;
                case JsonEvent.ObjectStart:
                    success = true;
                    return true;
                default:
                    success = false;
                    ErrorIncompatible<T>(mapper.DataTypeName(), mapper, out success);
                    // reader.ErrorNull("Expect { or null. Got Event: ", ev);
                    return false;
            }
        }

        public T ReadElement<T>(TypeMapper<T> mapper, ref T value, out bool success) {
            return mapper.Read(ref this, value, out success);
        }
        
        public TVal ErrorIncompatible<TVal>(TypeMapper objectMapper, PropField field, out bool success) {
            ErrorIncompatible<bool>(objectMapper.DataTypeName(), $" field: {field.name}", field.fieldType, out success);
            return default;
        }
      
        public PropField<T> GetField<T>(PropertyFields<T> propFields) {
            PropField<T> field = propFields.GetField(this.parser.key);
            if (field != null)
                return field;
            parser.SkipEvent();
            return null;
        }
        
        public PropField GetField32(PropertyFields propFields) {
            searchKey.FromBytes(parser.key);
            for (int n = 0; n < propFields.count; n++) {
                if (!searchKey.IsEqual(ref propFields.names32[n]))
                    continue;
                return propFields.fields[n];
            }
            parser.SkipEvent();
            return null;
        }
        
    }
}