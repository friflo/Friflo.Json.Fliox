// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Flow.Schema.Definition
{
    /// <summary>
    /// Contains the all required data to generate code for all types.
    /// Note: This file does and must not have any dependency to <see cref="System.Type"/>.
    /// </summary>
    public abstract class TypeSchema
    {
        public abstract     ICollection<TypeDef>    Types           { get; }
        public abstract     StandardTypes           StandardTypes   { get; }
        public abstract     ICollection<TypeDef>    SeparateTypes   { get; }
    }
}