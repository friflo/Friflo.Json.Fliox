// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Mapper.Map
{
    delegate void ReadFieldDelegate<in T>(T obj, PropField field, ref Reader reader, out bool success);

    partial struct Reader
    {
        private TVal HandleEventGen<TVal>(TypeMapper mapper, out bool success) {
            switch (parser.Event) {
                case JsonEvent.ValueNull:
                    if (!mapper.isNullable)
                        return ErrorIncompatible<TVal>(mapper.DataTypeName(), mapper, out success);
                    success = true;
                    return default;
                
                case JsonEvent.Error:
                    const string msg2 = "requirement: error must be handled by owner. Add missing JsonEvent.Error case to its Mapper";
                    throw new InvalidOperationException(msg2);
                // return null;
                default:
                    return ErrorIncompatible<TVal>(mapper.DataTypeName(), mapper, out success);
            }
        }

        public int ReadInt32 (string name, PropField field, out bool success) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen<int>(field.fieldType, out success);
            return parser.ValueAsByte(out success);
        }
    }
}