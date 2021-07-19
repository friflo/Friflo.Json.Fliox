// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Schema
{
    public class EmitResult
    {
        internal readonly   TypeMapper      mapper;
        internal readonly   string          content;
        internal readonly   HashSet<Type>   customTypes;

        public   override   string      ToString() => mapper.type.Name;

        public EmitResult(TypeMapper mapper, string content, HashSet<Type> customTypes) {
            this.mapper         = mapper;
            this.content        = content;
            this.customTypes    = customTypes;
        }
    }
}