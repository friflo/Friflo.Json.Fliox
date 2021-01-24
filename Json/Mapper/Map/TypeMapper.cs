// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif

    public interface ITypeMapper : IDisposable {
    }
    
    public abstract class TypeMapper<TVal> : ITypeMapper
    {
        public  readonly    Type        type;
        public  readonly    bool        isNullable;
        public  readonly    VarType     varType;

        public TypeMapper(Type type, bool isNullable) {
            this.type = type;
            this.isNullable = isNullable;
        }
        
        public abstract     string  DataTypeName();
        public abstract     void    Write(JsonWriter writer, TVal slot);
        public abstract     TVal    Read(JsonReader reader, TVal slot, out bool success);

        public virtual      void    Dispose() { }
        public virtual      void    InitStubType(TypeStore typeStore) { }
    }
    
    public abstract class CollectionMapper<TVal, TElm> : TypeMapper<TVal>
    {
        public TypeMapper<TElm> map;
        
        CollectionMapper(Type type, bool isNullable, TypeMapper<TElm> elementMapper) : base(type, isNullable) {
            map = elementMapper;
        }
    }


#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public interface ITypeMatcher
    {
        ITypeMapper CreateStubType(Type type);
    }

}