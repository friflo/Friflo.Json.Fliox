// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Flow.Schema.Utils
{
    public class TypeContext
    {
        public readonly     Generator           generator;
        public readonly     HashSet<TypeDef>    imports;
        public readonly     TypeDef             owner;

        public override     string              ToString() => owner.Name;

        public TypeContext (Generator generator, HashSet<TypeDef> imports, TypeDef owner) {
            this.generator      = generator;
            this.imports        = imports;
            this.owner          = owner;
        }
    }
}