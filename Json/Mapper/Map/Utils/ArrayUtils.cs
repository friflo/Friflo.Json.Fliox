// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;


// ReSharper disable once CheckNamespace
namespace Friflo.Json.Mapper.Map
{
    public partial struct Reader {
        public bool IsElementNullable(TypeMapper arrayMapper, TypeMapper elementType, out bool success) {
            if (!elementType.isNullable) {
                ErrorIncompatible<bool>(arrayMapper.DataTypeName(), " element", elementType, out success);
                return false;
            }
            success = false;
            return true;
        }
        
        public bool StartArray<TVal, TElm>(CollectionMapper<TVal, TElm> map, out bool success) {
            var ev = parser.Event;
            switch (ev) {
                case JsonEvent.ValueNull:
                    if (map.isNullable) {
                        success = true;
                        return false;
                    }
                    ErrorIncompatible<TVal>(map.DataTypeName(), map, out success);
                    return default;
                case JsonEvent.ArrayStart:
                    success = true;
                    return true;
                default:
                    success = false;
                    ErrorIncompatible<TVal>(map.DataTypeName(), map, out success);
                    return false;
            }
        }

    }
}