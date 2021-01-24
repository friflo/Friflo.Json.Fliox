// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    
    
    public abstract class TypeMapper
    {
        public abstract string      DataTypeName();
        public abstract void        Write (JsonWriter writer, ref Var slot, StubType stubType);
        public abstract bool        Read  (JsonReader reader, ref Var slot, StubType stubType);
    }
    
    
    
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public interface ITypeMatcher
    {
        StubType CreateStubType(Type type);
    }

}