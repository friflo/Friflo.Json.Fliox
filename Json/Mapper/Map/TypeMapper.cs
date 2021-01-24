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

    public interface ITypeMapper : IDisposable
    {
        void    InitStubType(TypeStore typeStore);
        Type    GetNativeType();
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

        public virtual      Type    GetNativeType() { return type; }

        public abstract     string  DataTypeName();
        public abstract     void    Write(JsonWriter writer, TVal slot);
        public abstract     TVal    Read(JsonReader reader, TVal slot, out bool success);

        public virtual      void    Dispose() { }
        
        /// <summary>
        /// Need to be overriden, in case the derived <see cref="TypeMapper{TVal}"/> support <see cref="System.Type"/>'s
        /// as fields or elements returning a <see cref="TypeMapper{TVal}"/>.<br/>
        /// 
        /// In this case <see cref="InitStubType"/> is used to map a <see cref="System.Type"/> to a required
        /// <see cref="TypeMapper{TVal}"/> by calling <see cref="TypeStore.GetType(System.Type)"/> and storing the returned
        /// reference also in the created <see cref="TypeMapper{TVal}"/> instance.<br/>
        ///
        /// This enables deferred initialization of StubType references by their related Type to support circular type dependencies.
        /// The goal is to support also type hierarchies without a 'directed acyclic graph' (DAG) of type dependencies.
        /// </summary>
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
            ITypeMapper stubType = typeStore.GetType(elementTypeNative);
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