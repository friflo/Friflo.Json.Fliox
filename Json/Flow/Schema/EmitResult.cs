// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Schema
{
    public class EmitResult
    {
        internal    readonly    TypeMapper  mapper;
        internal    readonly    string      content;

        public EmitResult(TypeMapper mapper, string content) {
            this.mapper     = mapper;
            this.content    = content;
        }
    }
}