// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Schema
{
    public class Generator
    {
        public readonly    Type        type;
        public readonly    string      folder;
        public readonly    TypeStore   typeStore;
        
        
        public Generator (Type type, string folder, TypeStore typeStore) {
            this.type       = type;
            this.folder     = folder;
            this.typeStore  = typeStore;
        }
    }
}