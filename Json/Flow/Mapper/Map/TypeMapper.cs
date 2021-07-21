// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Flow.Mapper.Access;
using Friflo.Json.Flow.Mapper.Diff;
using Friflo.Json.Flow.Mapper.Map.Obj.Reflect;
using Friflo.Json.Flow.Mapper.Map.Utils;
using Friflo.Json.Flow.Mapper.MapIL.Obj;
using Friflo.Json.Flow.Transform.Select;

namespace Friflo.Json.Flow.Mapper.Map
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class TypeMapper : IDisposable
    {
        public  readonly    Type            type;
        public  readonly    bool            isNullable;
        public  readonly    bool            isValueType;
        public  readonly    Type            nullableUnderlyingType;
        public  readonly    bool            useIL;
        internal            InstanceFactory instanceFactory;
        internal            string          discriminant;

        public virtual      bool            IsComplex => false;
        public virtual      bool            IsArray => false;
        public virtual      int             Count(object array) => throw new InvalidOperationException("Count not applicable");


        // ReSharper disable once UnassignedReadonlyField - field ist set via reflection below to use make field readonly
        public  readonly    PropertyFields  propFields;
        public              ClassLayout     layout;  // todo make readonly


        protected TypeMapper(StoreConfig config, Type type, bool isNullable, bool isValueType) {
            this.type                   = type;
            this.isNullable             = isNullable;
            this.isValueType            = isValueType;
            this.nullableUnderlyingType = Nullable.GetUnderlyingType(type);
            bool isNull = nullableUnderlyingType != null || !type.IsValueType;
            if (isNull != isNullable)
                throw new InvalidOperationException("invalid parameter: isNullable");

            this.useIL                  = config != null && config.useIL && isValueType && !type.IsPrimitive;
        }

        public abstract void            Dispose();

        public virtual string           DataTypeName() { return type.Name; }

        public abstract void            InitTypeMapper(TypeStore typeStore);

        public abstract DiffNode        DiffObject(Differ differ, object left, object right);
        public virtual  void            PatchObject(Patcher patcher, object value) { }

        public virtual  void            MemberObject(Accessor accessor, object value, PathNode<MemberValue> node) {
            throw new InvalidOperationException("MemberObject() is intended only for classes");
        }

        public abstract void            TraceObject(Tracer tracer, object slot);
        
        public abstract void            WriteObject(ref Writer writer, object slot);
        public abstract object          ReadObject(ref Reader reader, object slot, out bool success);
        
        public abstract bool            IsValueNullIL(ClassMirror mirror, int primPos, int objPos);
        public abstract void            WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos);
        public abstract bool            ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos);
        
        public abstract object CreateInstance();
        
        public bool IsNull<T>(ref T value) {
            if (isValueType) {
                if (nullableUnderlyingType == null)
                    return false;
                return EqualityComparer<T>.Default.Equals(value, default);
            }
            return value == null;
        }
        // --- Schema / Code generation related methods --- 
        public virtual  TypeMapper      GetElementMapper    ()     { return null; }
        public virtual  List<string>    GetEnumValues       ()     { return null; }
        public virtual  TypeMapper      GetUnderlyingMapper (out TypeSemantic typeSemantic) {
            typeSemantic = TypeSemantic.None;
            return null;
        }
    }
    
public enum TypeSemantic {
    None,
    Entity,
    Reference
}
    
    
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class TypeMapper<TVal> : TypeMapper
    {
        protected TypeMapper(StoreConfig config, Type type, bool isNullable, bool isValueType) :
            base(config, type, isNullable, isValueType) {
        }
        
        protected TypeMapper() :
            base(null, typeof(TVal), TypeUtils.IsNullable(typeof(TVal)), false) {
        }

        public virtual  void        Trace       (Tracer     tracer, TVal slot) { }
        public abstract void        Write       (ref Writer writer, TVal slot);
        public abstract TVal        Read        (ref Reader reader, TVal slot, out bool success);

        public virtual  DiffNode    Diff        (Differ differ, TVal left, TVal right) {
            bool areEqual = EqualityComparer<TVal>.Default.Equals(left, right);
            if (areEqual)
                return null;
            return differ.AddNotEqual(left, right);
        }
        
        public override DiffNode    DiffObject  (Differ differ, object left, object right) {
            return Diff(differ, (TVal)left, (TVal)right);
        }

        public override void TraceObject(Tracer tracer, object slot) {
            Trace(tracer, (TVal)slot);
        }

        public override bool IsValueNullIL(ClassMirror mirror, int primPos, int objPos) {
            throw new InvalidOperationException("IsValueNullIL() not applicable");
        }
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            throw new InvalidOperationException("WriteValueIL() not applicable");
        }

        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            throw new InvalidOperationException("ReadValueIL() not applicable");
        }

        public override void WriteObject(ref Writer writer, object value) {
#if DEBUG
            if (value == null)
                throw new InvalidOperationException("WriteObject() value must not be null");
#endif
            Write(ref writer, (TVal) value);
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
    
    public class ConcreteTypeMatcher : ITypeMatcher
    {
        private readonly TypeMapper mapper;

        public ConcreteTypeMatcher(TypeMapper mapper) {
            this.mapper = mapper;
        }

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type != mapper.type)
                return null;
            return mapper;
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