// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Mapper.Map.Obj.Reflect;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.MapIL.Obj;

namespace Friflo.Json.Mapper.Map
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class TypeMapper : IDisposable
    {
        public  readonly    Type        type;
        public  readonly    bool        isNullable;
        public  readonly    bool        isValueType;
        public  readonly    Type        underlyingType;
        public  readonly    bool        useIL;
        
        // ReSharper disable once UnassignedReadonlyField - field ist set via reflection below to use make field readonly
        public  readonly PropertyFields propFields;
        public              ClassLayout layout;  // todo make readonly


        protected TypeMapper(StoreConfig config, Type type, bool isNullable, bool isValueType) {
            if (config == null)
                throw new InvalidOperationException("Expect config != null");
            this.type           = type;
            this.isNullable     = isNullable;
            this.isValueType    = isValueType;
            this.underlyingType = Nullable.GetUnderlyingType(type);
            this.useIL          = config.useIL && isValueType && !type.IsPrimitive;
        }

        public abstract void            Dispose();
            
        public abstract string          DataTypeName();
        public abstract void            InitTypeMapper(TypeStore typeStore);
        
        public abstract void            WriteObject(ref Writer writer, object slot);
        public abstract object          ReadObject(ref Reader reader, object slot, out bool success);
        
        public abstract void            WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos);
        public abstract bool            ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos);


        public abstract object          CreateInstance();
        
        public bool IsNull<T>(ref T value) {
            if (isValueType) {
                if (underlyingType == null)
                    return false;
                return EqualityComparer<T>.Default.Equals(value, default);
            }
            return value == null;
        }
    }
    
    public abstract class TypeMapper<TVal> : TypeMapper
    {
        protected TypeMapper(StoreConfig config, Type type, bool isNullable, bool isValueType) :
            base(config, type, isNullable, isValueType) {
        }

        public abstract void    Write       (ref Writer writer, TVal slot);
        public abstract TVal    Read        (ref Reader reader, TVal slot, out bool success);

        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            throw new InvalidOperationException("WriteField() not applicable");
        }

        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            throw new InvalidOperationException("WriteField() not applicable");
        }

        public override void WriteObject(ref Writer writer, object slot) {
            if (slot != null)
                Write(ref writer, (TVal) slot);
            else
                WriteUtils.AppendNull(ref writer);
        }

        public override object ReadObject(ref Reader reader, object slot, out bool success) {
            if (slot != null)
                return Read(ref reader, (TVal) slot, out success);
            return Read(ref reader, default, out success);
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
        /// This enables deferred initialization of TypeMapper to support circular type dependencies.
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
        TypeMapper MatchTypeMapper(Type type, StoreConfig config);
    }
    
    public static class TypeMapperUtils {
        /*
        public static object CreateGenericInstance(Type genericType, Type[] genericArgs) {
            var genericTypeArgs = genericType.MakeGenericType(genericArgs);
            return Activator.CreateInstance(genericTypeArgs);
        } */
        
        public static object CreateGenericInstance(Type genericType, Type[] genericArgs, object[] constructorParams) {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var genericTypeArgs = genericType.MakeGenericType(genericArgs);
            return Activator.CreateInstance(genericTypeArgs, flags, null, constructorParams, null);
        } 
    }

}