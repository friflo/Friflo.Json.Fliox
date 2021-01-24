// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
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

        object  CreateInstance();
    }
    
    public abstract class TypeMapper<TVal> : ITypeMapper
    {
        public  readonly    Type        type;
        public  readonly    bool        isNullable;
        public  readonly    VarType     varType;

        public TypeMapper(Type type, bool isNullable) {
            this.type       = type;
            this.isNullable = isNullable;
            varType         = Var.GetVarType(type);
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

        public virtual      object  CreateInstance() {
            return null;
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