// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Schema
{
    public class Generator
    {
        public  readonly    ICollection<Type>                       rootTypes;
        public  readonly    string                                  folder;
        public  readonly    IReadOnlyDictionary<Type, TypeMapper>   typeMappers;
        public  readonly    Dictionary<TypeMapper, EmitResult>      emitTypes = new Dictionary<TypeMapper, EmitResult>();

        public Generator (ICollection<Type> rootTypes, string folder, TypeStore typeStore) {
            this.rootTypes  = rootTypes;
            this.folder     = folder;
            typeMappers     = typeStore.GetTypeMappers();
        }

        public void AddEmitType(EmitResult emit) {
            emitTypes.Add(emit.mapper, emit);
        }
    }
}