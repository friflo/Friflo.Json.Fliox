// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Linq.Expressions;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Access;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Mapper.Map.Obj.Reflect;
using Friflo.Json.Fliox.Mapper.Map.Utils;
using Friflo.Json.Fliox.Mapper.Map.Val;
using Friflo.Json.Fliox.Mapper.MapIL.Obj;
using Friflo.Json.Fliox.Mapper.Utils;
using Friflo.Json.Fliox.Transform.Select;

namespace Friflo.Json.Fliox.Mapper.Map.Obj
{
    internal class ClassMatcher : ITypeMatcher {
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
            bool notInstantiatable = type.IsInterface || type.IsAbstract;
            if (type.IsClass || type.IsValueType || notInstantiatable) {
                var factory = InstanceFactory.GetInstanceFactory(type);
                if (notInstantiatable && factory == null)
                    throw new InvalidOperationException($"type requires instantiatable types by [Fri.Instance()] or [Fri.Polymorph()] on: {type}");
                
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
        private readonly    ConstructorInfo constructor;
        private readonly    Func<T>         createInstance;

        public  override    string          DataTypeName() { return "class"; }
        public  override    bool            IsComplex       => true;

        protected ClassMapper (StoreConfig config, Type type, ConstructorInfo constructor, InstanceFactory instanceFactory, bool isValueType) :
            base (config, type, TypeUtils.IsNullable(type), isValueType)
        {
            this.instanceFactory = instanceFactory;
            if (instanceFactory != null)
                return;
            this.constructor = constructor;
            var lambda = CreateInstanceExpression();
            createInstance = lambda.Compile();
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
            var fields = new PropertyFields(type, typeStore);
            FieldInfo fieldInfo = typeof(TypeMapper).GetField(nameof(propFields), BindingFlags.Public | BindingFlags.Instance);
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
        
        public override DiffNode Diff(Differ differ, T left, T right) {
            object leftObj = left; // box in case of a struct. This enables FieldInfo.GetValue() / SetValue() operating on struct also.
            object rightObj = right;
            TypeMapper classMapper = this;

            if (!isValueType) {
                Type leftType = left.GetType();
                if (type != leftType)
                    classMapper = differ.typeCache.GetTypeMapper(leftType);
                Type rightType = right.GetType();
                if (leftType != rightType)
                    return differ.AddNotEqual(left, right);
            }

            differ.PushParent(left, right);
            PropField[] fields = classMapper.propFields.fields;
            for (int n = 0; n < fields.Length; n++) {
                PropField field = fields[n];
                differ.PushMember(field);

                object leftField = field.GetField(leftObj);
                object rightField = field.GetField(rightObj);
                if (leftField != null || rightField != null) {
                    if (leftField != null && rightField != null) {
                        field.fieldType.DiffObject(differ, leftField, rightField);
                    } else {
                        differ.AddNotEqual(leftField, rightField);
                    }
                } // else: both null

                differ.Pop();
            }
            return differ.PopParent();
        }

        public override void PatchObject(Patcher patcher, object obj) {
            TypeMapper classMapper = this;
            Type objType = obj.GetType();
            if (type != objType)
                classMapper = patcher.typeCache.GetTypeMapper(objType);
            
            PropField[] fields = classMapper.propFields.fields;
            for (int n = 0; n < fields.Length; n++) {
                PropField field = fields[n];
                if (patcher.IsMember(field.key)) {
                    var value = field.GetField(obj); 
                    var action = patcher.DescendMember(field.fieldType, value, out object newValue);
                    if  (action == NodeAction.Assign)
                        field.SetField(obj, newValue, patcher.setMethodParams);
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

            PropertyFields fields = classMapper.propFields;
            var children = node.GetChildren();
            foreach (var child in children) {
                if (child.IsMember()) {
                    var field = fields.GetField(child.GetName());
                    if (field == null)
                        continue;
                    object elemVar = field.GetField(obj);
                    accessor.HandleResult(child, elemVar);
                    var fieldType = field.fieldType;
                    if (fieldType.IsComplex && elemVar != null)
                        fieldType.MemberObject(accessor, elemVar, child);
                }
            }
        }

        public override void Trace(Tracer tracer, T slot) {
            object objRef = slot; // box in case of a struct. This enables FieldInfo.GetValue() / SetValue() operating on struct also.
            TypeMapper classMapper = this;

            if (!isValueType) {
                Type objType = slot.GetType();
                if (type != objType)
                    classMapper = tracer.typeCache.GetTypeMapper(objType);
            }

            PropField[] fields = classMapper.propFields.fields;
            for (int n = 0; n < fields.Length; n++) {
                PropField field = fields[n];
                object elemVar = field.GetField(objRef);
                if (elemVar != null)
                    field.fieldType.TraceObject(tracer, elemVar);
            }
        }
        
        public override void Write(ref Writer writer, T slot) {
            int startLevel = writer.IncLevel();

            object objRef = slot; // box in case of a struct. This enables FieldInfo.GetValue() / SetValue() operating on struct also.
            TypeMapper classMapper = this;
            bool firstMember = true;

            if (!isValueType) { // && instanceFactory != null)   todo
                Type objType = slot.GetType();  // GetType() cost performance. May use a pre-check with isPolymorphic
                if (type != objType) {
                    classMapper = writer.typeCache.GetTypeMapper(objType);
                    writer.WriteDiscriminator(this, classMapper, ref firstMember);
                }
            }

            PropField[] fields = classMapper.propFields.fields;
            for (int n = 0; n < fields.Length; n++) {
                PropField field = fields[n];
                
                object elemVar = field.GetField(objRef);
                if (elemVar == null) {
                    if (writer.writeNullMembers) {
                        writer.WriteFieldKey(field, ref firstMember);
                        writer.AppendNull();
                    }
                } else {
                    writer.WriteFieldKey(field, ref firstMember); 
                    var fieldType = field.fieldType;
                    fieldType.WriteObject(ref writer, elemVar);
                    writer.FlushFilledBuffer();
                }
            }
            writer.WriteObjectEnd(firstMember);
            writer.DecLevel(startLevel);
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
                        return reader.ErrorMsg<TypeMapper>($"No instance created in InstanceFactory: ", factory.GetType().Name, out success);
                    classType = reader.typeCache.GetTypeMapper(obj.GetType());
                } else {
                    if (ev == JsonEvent.ValueString && reader.parser.key.IsEqualString(discriminator)) {
                        string discriminant = reader.parser.value.ToString();
                        obj = (T) factory.CreatePolymorph(discriminant);
                        if (classType.IsNull(ref obj))
                            return reader.ErrorMsg<TypeMapper>($"No [Fri.Polymorph] type declared for discriminant: '{discriminant}' on type: ", classType.type.FullName, out success);
                        classType = reader.typeCache.GetTypeMapper(obj.GetType());
                        parser.NextEvent();
                    } else
                        return reader.ErrorMsg<TypeMapper>($"Expect discriminator \"{discriminator}\": \"...\" as first JSON member for type: ", classType.type.FullName, out success);
                }
            } else {
                if (classType.IsNull(ref obj))
                    obj = (T) classType.CreateInstance();
            }
            success = true;
            return classType;
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            // Ensure preconditions are fulfilled
            if (!reader.StartObject(this, out success))
                return default;
                
            TypeMapper classType = this;
            classType = GetPolymorphType(ref reader, classType, ref slot, out success);
            if (!success)
                return default;
            object objRef = slot; // box in case of a struct. This enables FieldInfo.GetValue() / SetValue() operating on struct also.
            
            JsonEvent ev = reader.parser.Event;
            var fields = classType.propFields;

            while (true) {
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ValueNull:
                        PropField field;
                        if ((field = reader.GetField32(fields)) == null)
                            break;
                        TypeMapper fieldType = field.fieldType;
                        object fieldVal = field.GetField(objRef);
                        object curFieldVal = fieldVal;
                        fieldVal = fieldType.ReadObject(ref reader, fieldVal, out success);
                        if (!success)
                            return default;
                        //
                        if (!fieldType.isNullable && fieldVal == null)
                            return reader.ErrorIncompatible<T>(this, field, out success);
                        
                        if (curFieldVal != fieldVal)
                            field.SetField(objRef, fieldVal, reader.setMethodParams);
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
    }
}