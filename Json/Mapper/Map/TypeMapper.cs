// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public interface ITypeMapper : IDisposable
    {
        bool        IsNullable();
            
        void        InitStubType(TypeStore typeStore);
        Type        GetNativeType();
        
        void        WriteObject(JsonWriter writer,   object slot);
        object      ReadObject (JsonReader reader,   object slot, out bool success);
        
        PropField       GetField(ref Bytes fieldName);
        PropertyFields  GetPropFields();


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

        public virtual      bool    IsNullable() { return isNullable; }

        public abstract     string  DataTypeName();
        public abstract     void    Write(JsonWriter writer, TVal slot);
        public abstract     TVal    Read(JsonReader reader, TVal slot, out bool success);

        public void WriteObject(JsonWriter writer, object slot) {
            if (slot != null)
                Write(writer, (TVal) slot);
            else
                Write(writer, default);
        }

        public object ReadObject(JsonReader reader, object slot, out bool success) {
            if (slot != null)
                return Read(reader, (TVal) slot, out success);
            return Read(reader, default, out success);
        }

        public virtual PropField GetField(ref Bytes fieldName) {
            throw new InvalidOperationException("method not applicable");
        }
        
        public virtual PropertyFields GetPropFields() {
            throw new InvalidOperationException("method not applicable");
        }

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
    
    public static class TypeMapperUtils {
        public static object CreateGenericInstance(Type genericType, Type[] genericArgs) {
            var genericTypeArgs = genericType.MakeGenericType(genericArgs);
            return Activator.CreateInstance(genericTypeArgs);
        }
        
        public static object CreateGenericInstance(Type genericType, Type[] genericArgs, object[] constructorParams) {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var genericTypeArgs = genericType.MakeGenericType(genericArgs);
            return Activator.CreateInstance(genericTypeArgs, flags, null, constructorParams, null);
        } 
    }

}