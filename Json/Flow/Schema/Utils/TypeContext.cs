// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Schema.Utils
{
    public class TypeContext
    {
        public readonly     Generator       generator;
        public readonly     HashSet<ITyp>   imports;
        public readonly     ITyp            owner;

        public override     string          ToString() => owner.Name;

        public TypeContext (Generator generator, HashSet<ITyp> imports, ITyp owner) {
            this.generator      = generator;
            this.imports        = imports;
            this.owner          = owner;
        }
    }
}