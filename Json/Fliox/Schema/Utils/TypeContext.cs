// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema.Utils
{
    public class TypeContext
    {
        public readonly     Generator           generator;
        public readonly     HashSet<TypeDef>    imports;
        /// <summary>The <see cref="type"/> the context was created for. Each type gets its own context.</summary>
        public readonly     TypeDef             type;
        public readonly     StandardTypes       standardTypes;

        public override     string              ToString() => type.Name;

        public TypeContext (Generator generator, HashSet<TypeDef> imports, TypeDef type) {
            this.generator      = generator;
            this.imports        = imports;
            this.type           = type;
            standardTypes       = generator.standardTypes;
        }
    }
}