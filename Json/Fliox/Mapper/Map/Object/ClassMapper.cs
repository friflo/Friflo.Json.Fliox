// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Linq.Expressions;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Access;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;
using Friflo.Json.Fliox.Mapper.Map.Utils;
using Friflo.Json.Fliox.Mapper.Map.Val;
using Friflo.Json.Fliox.Mapper.MapIL.Obj;
using Friflo.Json.Fliox.Mapper.Utils;
using Friflo.Json.Fliox.Transform.Select;

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
                
                object[] constructorParams = {config, type, constructor, factory, type.IsValueType};
#if !UNITY_5_3_OR_NEWER
                if (config.useIL) {
                    if (type.IsValueType) {
                        // new StructMapper<T>(config, type, constructor);
                        return (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(StructILMapper<>), new[] {type}, constructorParams);
                    }
                    // new ClassILMapper<T>(config, type, constructor);
                    return (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(ClassILMapper<>), new[] {type}, constructorParams);
                }
#endif
                // new ClassMapper<T>(config, type, constructor);
                return (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(ClassMapper<>), new[] {type}, constructorParams);
            }
            return null;
        }
    }
    
    internal class ClassMapper<T> : TypeMapper<T> {
        private readonly    ConstructorInfo     constructor;
        private readonly    Func<T>             createInstance;

        public  override    string              DataTypeName() { return $"class {typeof(T).Name}"; }
        public  override    bool                IsComplex       => true;
        // ReSharper disable once UnassignedReadonlyField - field ist set via reflection below to use make field readonly
        public  readonly    PropertyFields<T>   propFields;
        
        public  override    PropertyFields      PropFields => propFields;

        protected ClassMapper (StoreConfig config, Type type, ConstructorInfo constructor, InstanceFactory instanceFactory, bool isValueType) :
            base (config, type, TypeUtils.IsNullable(type), isValueType)
        {
            this.instanceFactory = instanceFactory;
            if (instanceFactory != null)
                return;
            this.constructor = constructor;
            var lambda      = CreateInstanceExpression();
            createInstance  = lambda.Compile();
            propFields      = null; // suppress [CS0649] Field '...' is never assigned to, and will always have its default value null
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
        
        public override void InitTypeMapper(TypeStore typeStore) {
            instanceFactory?.InitFactory(typeStore);
            var query   = new FieldQuery<T>(typeStore, type);
            var fields  = new PropertyFields<T>(query);
            FieldInfo fieldInfo = mapperType.GetField(nameof(propFields), BindingFlags.Public | BindingFlags.Instance);
            // ReSharper disable once PossibleNullReferenceException
            fieldInfo.SetValue(this, fields);
        }
        
        public override object CreateInstance() {
            if (instanceFactory != null)
                return instanceFactory.CreateInstance(typeof(T));
            
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
        
        internal override void WriteEntityKey(ref Writer writer, object obj) {
            var keyField        = propFields.keyField;
            if (keyField == null) throw new InvalidOperationException($"missing [Key] field in Type {typeof(T).Name}");
            var elemVar         = keyField.member.GetVar(obj);
            bool firstMember    = true;
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


        protected static TypeMapper GetPolymorphType(ref Reader reader, TypeMapper classType, ref T obj, out bool success) {
            ref var parser = ref reader.parser;
            var ev = parser.NextEvent();

            var factory = classType.instanceFactory;
            if (factory != null) {
                string discriminator = factory.discriminator;
                if (discriminator == null) {
                    obj = (T) factory.CreateInstance(typeof(T));
                    if (classType.IsNull(ref obj))
                        return reader.ErrorMsg<TypeMapper<T>>($"No instance created in InstanceFactory: ", factory.GetType().Name, out success);
                    classType = reader.typeCache.GetTypeMapper(obj.GetType());
                } else {
                    if (ev == JsonEvent.ValueString && reader.parser.key.IsEqualString(discriminator)) {
                        string discriminant = reader.parser.value.AsString();
                        obj = (T) factory.CreatePolymorph(discriminant);
                        if (classType.IsNull(ref obj))
                            return reader.ErrorMsg<TypeMapper<T>>($"No [PolymorphType] type declared for discriminant: '{discriminant}' on type: ", classType.type.Name, out success);
                        classType = reader.typeCache.GetTypeMapper(obj.GetType());
                        parser.NextEvent();
                    } else
                        return reader.ErrorMsg<TypeMapper<T>>($"Expect discriminator '{discriminator}': '...' as first JSON member for type: ", classType.type.Name, out success);
                }
                success = true;
                return classType;
            }
            if (classType.IsNull(ref obj))
                obj = (T) classType.CreateInstance();
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
                return (T)subType.ReadObject(ref reader, slot, out success);
            }
            return (T)ReadObject(ref reader, slot, out success);
        }
        
        internal override object ReadObject(ref Reader reader, object slot, out bool success)
        {
            object objRef = slot; // box in case of a struct. This enables FieldInfo.GetValue() / SetValue() operating on struct also.
            
            JsonEvent ev = reader.parser.Event;
            var fields = propFields;

            while (true) {
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ValueNull:
                        PropField<T> field;
                        if ((field = reader.GetField(fields)) == null)
                            break;
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
        
        private void SetDstInstance (T src, ref T dst) {
            var srcType = src.GetType();
            if (dst == null || srcType != dst.GetType()) {
                var factory = instanceFactory;
                if (factory == null) {
                    dst = (T)CreateInstance();
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
                    member.SetVar(dst, field.varType.DefaultValue);
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