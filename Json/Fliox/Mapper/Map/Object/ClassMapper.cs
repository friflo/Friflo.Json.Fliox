// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Access;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Mapper.Gen;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;
using Friflo.Json.Fliox.Mapper.Map.Utils;
using Friflo.Json.Fliox.Mapper.Map.Val;
using Friflo.Json.Fliox.Mapper.Utils;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Transform.Select;
using static Friflo.Json.Fliox.Mapper.Map.TypeMapperUtils;

namespace Friflo.Json.Fliox.Mapper.Map.Object
{
    internal sealed class ClassMatcher : ITypeMatcher {
        public static readonly ClassMatcher Instance = new ClassMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // don't handle standard types
                return null;
            Type nullableStruct = TypeUtils.GetNullableStruct(type);
            if (nullableStruct == null && TypeUtils.IsGenericType(type)) // don't handle generic types like List<> or Dictionary<,>
                return null;
            if (EnumMatcher.IsEnum(type, out bool _))
                return null;
           
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            bool notInstantiable = type.IsInterface || type.IsAbstract;
            if (type.IsClass || type.IsValueType || notInstantiable) {
                var factory = InstanceFactory.GetInstanceFactory(type);
                if (notInstantiable && factory == null)
                    throw new InvalidOperationException($"type requires concrete types by [InstanceType()] or [PolymorphType()] on: {type}");
                
                var underlyingType  = Nullable.GetUnderlyingType(type);
                var genType         = underlyingType ?? type;
                object[] constructorParams = {config, type, constructor, factory, type.IsValueType, null};

                var genClassName    = $"Gen.{genType.Namespace}.Gen_{genType.Name}";
                var genClass        = genType.Assembly.GetType(genClassName);
                if (genClass != null) {
                    var flags               = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                    MethodInfo genWrite     = genClass.GetMethod("Write", flags);
                    MethodInfo genReadField = genClass.GetMethod("ReadField", flags);
                    constructorParams = new object[] {config, type, constructor, factory, type.IsValueType, genClass, genWrite, genReadField };
                    if (underlyingType != null) {
                        return (TypeMapper) CreateGenericInstance(typeof(StructNullMapperGen<>), new[] {underlyingType}, constructorParams);
                    }
                    if (type.IsValueType) {
                        return (TypeMapper) CreateGenericInstance(typeof(StructMapperGen<>), new[] {type}, constructorParams);
                    }
                    // new ClassMapperGen<T>(config, type, constructor);    
                    return (TypeMapper) CreateGenericInstance(typeof(ClassMapperGen<>), new[] {type}, constructorParams);
                }
                // new ClassMapper<T>(config, type, constructor);
                return (TypeMapper) CreateGenericInstance(typeof(ClassMapper<>), new[] {type}, constructorParams);
            }
            return null;
        }
    }
    
    internal class ClassMapper<T> : TypeMapper<T> {
        private readonly    ConstructorInfo     constructor;
        private readonly    Func<T>             createInstance;
        private readonly    Type                genClass;


        public  override    string              DataTypeName()      => $"class {typeof(T).Name}";
        public  override    bool                IsComplex           => true;
        public  override    StandardTypeId      StandardTypeId      => StandardTypeId.Object;
        // ReSharper disable once UnassignedReadonlyField - field ist set via reflection below to use make field readonly
        public  readonly    PropertyFields<T>   propFields;
        
        public  override    PropertyFields      PropFields => propFields;

        protected ClassMapper (
            StoreConfig     config,
            Type            type,
            ConstructorInfo constructor,
            InstanceFactory instanceFactory,
            bool            isValueType,
            Type            genClass)
            : base (config, type, TypeUtils.IsNullable(type), isValueType)
        {
            this.instanceFactory = instanceFactory;
            if (instanceFactory != null)
                return;
            this.constructor = constructor;
            var lambda      = CreateInstanceExpression();
            createInstance  = lambda.Compile();
            propFields      = null; // suppress [CS0649] Field '...' is never assigned to, and will always have its default value null
            this.genClass   = genClass;        
        }
        
        public override void Dispose() {
            base.Dispose();
            propFields?.Dispose();
        }
        
        public override Type BaseType { get {
            var baseType        = type.BaseType;
            bool isDerived      = baseType != typeof(object);
            bool isStruct       = baseType == typeof(ValueType);
            if (isDerived && !isStruct)
                return baseType;
            return null;
        }}

        private static Expression<Func<T>> CreateInstanceExpression () {
            Type nullableStruct = TypeUtils.GetNullableStruct(typeof(T));
            Expression create;
            if (nullableStruct != null) {
                Expression newStruct = Expression.New(nullableStruct);
                create = Expression.Convert(newStruct, typeof(T));
            } else {
                create = Expression.New(typeof(T));
            }
            return Expression.Lambda<Func<T>> (create);
        }
        
        public override bool IsNull(ref T value) {
            if (isValueType) {
                if (nullableUnderlyingType == null)
                    return false;
                return EqualityComparer<T>.Default.Equals(value, default);
            }
            return value == null;
        }
        
        public override void InitTypeMapper(TypeStore typeStore) {
            instanceFactory?.InitFactory(typeStore);
            var query   = new FieldQuery<T>(typeStore, type, genClass);
            var fields  = new PropertyFields<T>(query);
            FieldInfo fieldInfo = mapperType.GetField(nameof(propFields), BindingFlags.Public | BindingFlags.Instance);
            // ReSharper disable once PossibleNullReferenceException
            fieldInfo.SetValue(this, fields);
        }
        
        public override object NewInstance() {
            if (instanceFactory != null)
                return instanceFactory.CreateInstance(null, typeof(T));
            
            if (createInstance != null)
                return createInstance();
            
            if (constructor == null) {
                // Is it a struct?
                if (type.IsValueType)
                    return Activator.CreateInstance(type);
                throw new InvalidOperationException("No default constructor available for: " + type.Name);
            }
            return ReflectUtils.CreateInstance(constructor);
        }
        
        // ----------------------------------- Write / Read -----------------------------------
        
        public override DiffType Diff(Differ differ, T left, T right) {
            TypeMapper classMapper = this;

            if (!isValueType) {
                Type leftType = left.GetType();
                if (type != leftType)
                    classMapper = differ.TypeCache.GetTypeMapper(leftType);
                Type rightType = right.GetType();
                if (leftType != rightType)
                    return differ.AddNotEqualObject(left, right);
                return classMapper.DiffObject(differ, left, right);
            }
            return DiffObject(differ, left, right);
        }
        
        internal override DiffType DiffObject(Differ differ, object left, object right)
        {
            // boxing left & right support modifying a struct. This enables FieldInfo.GetValue() / SetValue() operating on struct also.
            differ.PushParent(left, right);
            var fields = propFields.typedFields;
            for (int n = 0; n < fields.Length; n++) {
                var field = fields[n];
                differ.PushMember(field);

                Var leftField   = field.member.GetVar(left);
                Var rightField  = field.member.GetVar(right);
                field.fieldType.DiffVar(differ, leftField, rightField);

                differ.Pop();
            }
            return differ.PopParent();
        }

        public override void PatchObject(Patcher patcher, object obj) {
            TypeMapper classMapper = this;
            Type objType = obj.GetType();
            if (type != objType)
                classMapper = patcher.TypeCache.GetTypeMapper(objType);
            
            var fields = classMapper.PropFields.fields; // todo use PropertyFields<>.typedFields to utilize PropField<>
            for (int n = 0; n < fields.Length; n++) {
                var field = fields[n];
                if (patcher.IsMember(field.key)) {
                    Var value   = field.member.GetVar(obj); 
                    var action  = patcher.DescendMember(field.fieldType, value, out Var newValue);
                    if  (action == NodeAction.Assign)
                        field.member.SetVar(obj, newValue);
                    else
                        throw new InvalidOperationException($"NodeAction not applicable: {action}");
                    return;
                }
            }
        }

        public override void MemberObject(Accessor accessor, object obj, PathNode<MemberValue> node) {
            TypeMapper classMapper = this;
            Type objType = obj.GetType();
            if (type != objType)
                classMapper = accessor.TypeCache.GetTypeMapper(objType);

            var fields = classMapper.PropFields; // todo use PropertyFields<>.typedFields to utilize PropField<>
            var children = node.GetChildren();
            foreach (var child in children) {
                if (child.IsMember()) {
                    var field = fields.GetPropField(child.GetName());
                    if (field == null)
                        continue;
                    Var elemVar = field.member.GetVar(obj);
                    accessor.HandleResult(child, elemVar.ToObject());
                    var fieldType = field.fieldType;
                    if (fieldType.IsComplex && elemVar.NotNull)
                        fieldType.MemberObject(accessor, elemVar.Object, child);
                }
            }
        }

        public override void Write(ref Writer writer, T slot) {
            int startLevel = writer.IncLevel();

            TypeMapper classMapper = this;
            bool firstMember = true;

            if (!isValueType) { // && instanceFactory != null)   todo
                Type objType = slot.GetType();  // GetType() cost performance. May use a pre-check with isPolymorphic
                if (type != objType) {
                    classMapper = writer.typeCache.GetTypeMapper(objType);
                    writer.WriteDiscriminator(this, classMapper, ref firstMember);
                }
            }
            classMapper.WriteObject(ref writer, slot, ref firstMember);

            writer.WriteObjectEnd(firstMember);
            writer.DecLevel(startLevel);
        }
        
        internal override void WriteEntityKey(ref Writer writer, object obj, ref bool firstMember) {
            var keyField        = propFields.keyField;
            if (keyField == null) throw new InvalidOperationException($"missing [Key] field in Type {typeof(T).Name}");
            var elemVar         = keyField.member.GetVar(obj);
            writer.WriteFieldKey(keyField, ref firstMember);
            keyField.fieldType.WriteVar(ref writer, elemVar);
            writer.FlushFilledBuffer();
        }
        
        internal override void WriteObject(ref Writer writer, object slot, ref bool firstMember)
        {
            object objRef = slot; // box in case of a struct. This enables FieldInfo.GetValue() / SetValue() operating on struct also.
            var fields = propFields.typedFields;
            for (int n = 0; n < fields.Length; n++) {
                var field = fields[n];
                
                var elemVar     = field.member.GetVar(objRef);
                var fieldType   = field.fieldType;
                bool isNull     = fieldType.IsNullVar(elemVar);
                if (isNull) {
                    if (writer.writeNullMembers) {
                        writer.WriteFieldKey(field, ref firstMember);
                        writer.AppendNull();
                    }
                } else {
                    writer.WriteFieldKey(field, ref firstMember); 
                    fieldType.WriteVar(ref writer, elemVar);
                    writer.FlushFilledBuffer();
                }
            }
        }

        protected static TypeMapper GetPolymorphType(ref Reader reader, ClassMapper<T> classType, ref T obj, out bool success) {
            ref var parser = ref reader.parser;
            var ev = parser.NextEvent();

            var factory = classType.instanceFactory;
            if (factory != null) {
                string discriminator = factory.discriminator;
                if (discriminator == null) {
                    if (obj == null) {
                        obj = (T) factory.CreateInstance(reader.readerPool, typeof(T));
                        if (obj == null)
                            return reader.ErrorMsg<TypeMapper<T>>($"No instance created in InstanceFactory: ", factory.GetType().Name, out success);
                    } else {
                        // reuse passed obj
                    }
                    success = true;
                    return factory.InstanceMapper;
                }
                if (ev == JsonEvent.ValueString && reader.parser.key.IsEqualArray(factory.discriminatorBytes)) {
                    ref Bytes discriminant = ref reader.parser.value;
                    obj = (T) factory.CreatePolymorph(reader.readerPool, discriminant, obj, out var mapper);
                    if (obj == null)
                        return reader.ErrorMsg<TypeMapper<T>>($"No [PolymorphType] type declared for discriminant: '{discriminant}' on type: ", classType.type.Name, out success);
                    parser.NextEvent();
                    success = true;
                    return mapper;
                }
                return reader.ErrorMsg<TypeMapper<T>>($"Expect discriminator '{discriminator}': '...' as first JSON member for type: ", classType.type.Name, out success);
            }
            if (classType.IsNull(ref obj))
                obj = (T) classType.CreateInstance(reader.readerPool);
            success = true;
            return null;
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            // Ensure preconditions are fulfilled
            if (!reader.StartObject(this, out success))
                return default;

            var subType = GetPolymorphType(ref reader, this, ref slot, out success);
            if (!success)
                return default;
            if (subType != null) {
                // case: typeof(T) is a class
                return (T)subType.ReadObject(ref reader, slot, out success);
            }
            var result = ReadObject(ref reader, slot, out success);
            // returned result is null in case of success == false.
            if (success) {
                return (T)result;
            }
            // return default in error case as T can be a struct
            return default;
        }
        
        internal override object ReadObject(ref Reader reader, object slot, out bool success)
        {
            object objRef = slot; // box in case of a struct. This enables FieldInfo.GetValue() / SetValue() operating on struct also.
            
            JsonEvent   ev      = reader.parser.Event;
            Span<bool>  found   = reader.setMissingFields ? stackalloc bool [GetFoundCount()] : default;

            while (true) {
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ValueNull:
                        PropField<T> field;
                        if ((field = reader.GetField(propFields)) == null)
                            break;
                        if (reader.setMissingFields) found[field.fieldIndex] = true;
                        TypeMapper fieldType = field.fieldType;
                        Var fieldVal    = field.member.GetVar(objRef);
                        Var curFieldVal = fieldVal;
                        fieldVal        = fieldType.ReadVar(ref reader, fieldVal, out success);
                        if (!success)
                            return default;
                        //
                        if (!fieldType.isNullable && fieldVal.IsNull)
                            return reader.ErrorIncompatible<T>(this, field, out success);
                        
                        if (curFieldVal != fieldVal)
                            field.member.SetVar(objRef, fieldVal);
                        break;

                    case JsonEvent.ObjectEnd:
                        if (reader.setMissingFields) ClearReadToFields(objRef, found);
                        success = true;
                        return (T)objRef;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return reader.ErrorMsg<T>("unexpected state: ", ev, out success);
                }
                ev = reader.parser.NextEvent();
            }
        }
        
        protected int GetFoundCount() {
            return propFields.count;
        }

        protected void ClearReadToFields(object obj, in Span<bool> found) {
            for (int n = 0; n < propFields.count; n++) {
                if (found[n])
                    continue;
                var missingField = propFields.fields[n];
                missingField.member.SetVar(obj, missingField.defaultValue);
            }
        }
        
        private void SetDstInstance (T src, ref T dst) {
            var srcType = src.GetType();
            if (dst == null || srcType != dst.GetType()) {
                var factory = instanceFactory;
                if (factory == null) {
                    dst = (T)NewInstance();
                } else {
                    dst = (T)factory.CreatePolymorph(srcType);
                }
            }
        }
        
        public override void CopyVar(in Var src, ref Var dst) {
            var srcObject   = (T)src.TryGetObject();
            T   dstObject;
            if (isValueType) {
                dstObject   = default;
            } else {
                dstObject   = (T)dst.TryGetObject();
            }
            Copy(srcObject, ref dstObject);
            dst             = new Var(dstObject);
        }
        
        public override void Copy(T src, ref T dst) {
            if (!isValueType) {
                SetDstInstance(src, ref dst);
            }
            var fields = propFields.typedFields;
            for (int n = 0; n < fields.Length; n++) {
                var field           = fields[n];
                var member          = field.member;
                var srcMemberVar    = member.GetVar(src);
                if (srcMemberVar.IsNull) {
                    member.SetVar(dst, field.defaultValue);
                    continue;
                }
                var fieldType       = field.fieldType;
                Var dstMemberVar    = new Var();
                if (!fieldType.isValueType) {
                    dstMemberVar    = member.GetVar(dst);
                }
                fieldType.CopyVar(srcMemberVar, ref dstMemberVar);
                member.SetVar(dst, dstMemberVar);
            }
        }
    }
}