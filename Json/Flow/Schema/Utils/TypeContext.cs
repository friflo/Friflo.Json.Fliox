// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Schema.Utils
{
    public class TypeContext
    {
        public readonly     HashSet<Type>   imports;
        public readonly     TypeMapper      owner;
        public readonly     TypeSemantic    semantic;
        
        public TypeContext (HashSet<Type> imports, TypeMapper owner, TypeSemantic semantic) {
            this.imports    = imports;
            this.owner      = owner;
            this.semantic   = semantic;
        }
    }
}