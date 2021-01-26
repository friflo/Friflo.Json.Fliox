// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Class;
using Friflo.Json.Mapper.Class.IL;

namespace Friflo.Json.Mapper.Map
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class TypeMapper : IDisposable
    {
        public  readonly    Type        type;
        public  readonly    bool        isNullable;

        protected TypeMapper(Type type, bool isNullable) {
            this.type       = type;
            this.isNullable = isNullable;
        }

        public abstract void            Dispose();
            
        public abstract string          DataTypeName();
        public abstract void            InitTypeMapper(TypeStore typeStore);
        
        public abstract void            WriteObject (JsonWriter writer,   object slot);
        public abstract object          ReadObject  (JsonReader reader,   object slot, out bool success);
        
        public abstract  void           WriteField  (JsonWriter writer, ClassPayload payload, PropField field);
        public abstract  bool           ReadField   (JsonReader reader, ClassPayload payload, PropField field);

        
        public abstract PropField       GetField(ref Bytes fieldName);
        public abstract PropertyFields  GetPropFields();


        public abstract object          CreateInstance();
    }
    
    public abstract class TypeMapper<TVal> : TypeMapper
    {
        protected TypeMapper(Type type, bool isNullable) :
            base (type, isNullable)
        {
        }

        public abstract void    Write       (JsonWriter writer, TVal slot);
        public abstract TVal    Read        (JsonReader reader, TVal slot, out bool success);

        
        public override void WriteField (JsonWriter writer, ClassPayload payload, PropField field) {
            throw new InvalidOperationException("WriteField() not applicable");
        }

        public override bool ReadField  (JsonReader reader, ClassPayload payload, PropField field) {
            throw new InvalidOperationException("WriteField() not applicable");
        }

        public override void WriteObject(JsonWriter writer, object slot) {
            if (slot != null)
                Write(writer, (TVal) slot);
            else
                Write(writer, default);
        }

        public override object ReadObject(JsonReader reader, object slot, out bool success) {
            if (slot != null)
                return Read(reader, (TVal) slot, out success);
            return Read(reader, default, out success);
        }

        public override PropField GetField(ref Bytes fieldName) {
            throw new InvalidOperationException("method not applicable");
        }
        
        public override PropertyFields GetPropFields() {
            throw new InvalidOperationException("method not applicable");
        }

        public override      void    Dispose() { }
        
        /// <summary>
        /// Need to be overriden, in case the derived <see cref="TypeMapper{TVal}"/> support <see cref="System.Type"/>'s
        /// as fields or elements returning a <see cref="TypeMapper{TVal}"/>.<br/>
        /// 
        /// In this case <see cref="InitTypeMapper"/> is used to map a <see cref="System.Type"/> to a required
        /// <see cref="TypeMapper{TVal}"/> by calling <see cref="TypeStore.GetTypeMapper"/> and storing the returned
        /// reference also in the created <see cref="TypeMapper{TVal}"/> instance.<br/>
        ///
        /// This enables deferred initialization of StubType references by their related Type to support circular type dependencies.
        /// The goal is to support also type hierarchies without a 'directed acyclic graph' (DAG) of type dependencies.
        /// </summary>
        public override      void    InitTypeMapper(TypeStore typeStore) { }

        public override      object  CreateInstance() {
            return null;
        }
    }
    


#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public interface ITypeMatcher
    {
        TypeMapper MatchTypeMapper(Type type, ResolverConfig config);
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