// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Mapper.Map;

namespace Friflo.Json.Mapper.Types
{
    public class PrimitiveType
    {
        public static bool IsPrimitiveNullable(Type type) {
            return Nullable.GetUnderlyingType(type) != null;
        }
    }
}