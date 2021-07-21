// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Schema.Utils
{
    public class EmitType
    {
        /// the mapper assigned to the type
        internal readonly   TypeMapper      mapper;
        /// the piece of code to define the type
        internal readonly   string          content;
        /// contain type imports directly used by this type / mapper. 
        internal readonly   HashSet<Type>   imports;

        public   override   string      ToString() => mapper.type.Name;

        public EmitType(TypeMapper mapper, string content, HashSet<Type> imports) {
            this.mapper     = mapper;
            this.content    = content;
            this.imports    = imports;
        }
    }
}