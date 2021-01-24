// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
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
        public              TypeMapper<TElm>    elementType;  // todo rename to mapElement
        public   readonly   Type                keyType;
        public   readonly   int                 rank;
        // ReSharper disable once UnassignedReadonlyField
        // field ist set via reflection below to enable using a readonly field
        private  readonly   Type                elementTypeNative;
        public   readonly   VarType             elementVarType;
        internal readonly   ConstructorInfo     constructor;

        internal CollectionMapper (
            Type                type,
            Type                elementType,
            int                 rank,
            Type                keyType,
            ConstructorInfo     constructor) : base (type, true)
        {
            this.keyType        = keyType;
            elementTypeNative   = elementType;
            if (elementType == null)
                throw new NullReferenceException("elementType is required");
            this.rank           = rank;
            elementVarType       = Var.GetVarType(elementType);
            // constructor can be null. E.g. All array types have none.
            this.constructor    = constructor;
        }
        
        public override void InitStubType(TypeStore typeStore) {
            FieldInfo fieldInfo = GetType().GetField(nameof(elementType));
            StubType stubType = typeStore.GetType(elementTypeNative);
            // ReSharper disable once PossibleNullReferenceException
            fieldInfo.SetValue(this, stubType);
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