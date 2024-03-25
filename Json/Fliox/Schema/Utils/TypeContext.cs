// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Language;

namespace Friflo.Json.Fliox.Schema.Utils
{
    public sealed class TypeContext
    {
        public readonly     Generator           generator;
        public readonly     HashSet<TypeDef>    imports;
        /// <summary>The <see cref="type"/> the context was created for. Each type gets its own context.</summary>
        public readonly     TypeDef             type;
        public readonly     StandardTypes       standardTypes;
        /// should be used rarely. Use StringBuilder created in Generate() methods instead
        public readonly     StringBuilder       sb = new StringBuilder(); 

        public override     string              ToString() => type.Name;

        public TypeContext (Generator generator, HashSet<TypeDef> imports, TypeDef type) {
            this.generator      = generator;
            this.imports        = imports;
            this.type           = type;
            standardTypes       = generator.standardTypes;
        }
    }
}