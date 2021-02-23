// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Obj.Reflect;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Map.Val;
using Friflo.Json.Mapper.MapIL.Obj;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Obj
{
    public class ClassMatcher : ITypeMatcher {
        public static readonly ClassMatcher Instance = new ClassMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            Type nullableStruct = TypeUtils.GetNullableStruct(type);
            if (nullableStruct == null && TypeUtils.IsGenericType(type)) // dont handle generic types like List<> or Dictionary<,>
                return null;
            if (EnumMatcher.IsEnum(type, out bool _))
                return null;
           
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            if (type.IsClass || type.IsValueType || type.IsInterface) {
                InstanceFactory factory = null;
                Type factoryType = GetInstanceFactoryType(type);
                if (factoryType != null) {
                    factory = (InstanceFactory)ReflectUtils.CreateInstance(factoryType);
                }
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

        private static Type GetInstanceFactoryType(Type type) {
            foreach (var attr in type.CustomAttributes) {
                if (attr.AttributeType == typeof(JsonTypeAttribute)) {
                    if (attr.NamedArguments != null) {
                        foreach (var arg in attr.NamedArguments) {
                            if (arg.MemberName == nameof(JsonTypeAttribute.InstanceFactory)) {
                                if (arg.TypedValue.Value != null) {
                                    var instanceFactory = arg.TypedValue.Value as Type;
                                    if (instanceFactory != null && instanceFactory.IsSubclassOf(typeof(InstanceFactory)))
                                        return instanceFactory;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class ClassMapper<T> : TypeMapper<T> {
        private readonly ConstructorInfo    constructor;
        private readonly Func<T>            createInstance;

        public override string DataTypeName() { return "class"; }

       
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
            propFields.Dispose();
        }

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
            var fields = new PropertyFields(type, typeStore);
            FieldInfo fieldInfo = typeof(TypeMapper).GetField(nameof(propFields), BindingFlags.Public | BindingFlags.Instance);
            // ReSharper disable once PossibleNullReferenceException
            fieldInfo.SetValue(this, fields);
        }
        
        public override object CreateInstance(string discriminant) {
            if (instanceFactory != null)
                return instanceFactory.CreateObject(discriminant);
            
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
                writer.WriteMemberKey(field, ref firstMember); 
                
                object elemVar = field.GetField(objRef);
                if (elemVar == null) {
                    writer.AppendNull();
                } else {
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
                string discriminator = factory.Discriminator;
                if (discriminator == null) {
                    obj = (T) classType.CreateInstance(null);
                    classType = reader.typeCache.GetTypeMapper(obj.GetType());
                } else {
                    if (ev == JsonEvent.ValueString && reader.parser.key.IsEqualString(discriminator)) {
                        string discriminant = reader.parser.value.ToString();
                        obj = (T) classType.CreateInstance(discriminant);
                        if (classType.IsNull(ref obj))
                            return reader.ErrorMsg<TypeMapper>($"No instance created with name: '{discriminant}' in InstanceFactory: ", factory.GetType().Name, out success);
                            
                        classType = reader.typeCache.GetTypeMapper(obj.GetType());
                        parser.NextEvent();
                    } else
                        return reader.ErrorMsg<TypeMapper>($"Expect discriminator \"{discriminator}\": \"...\" as first JSON member when using InstanceFactory: ", factory.GetType().Name, out success);
                }
            } else {
                if (classType.IsNull(ref obj))
                    obj = (T) classType.CreateInstance(null);
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
            var propFields = classType.propFields;

            while (true) {
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        PropField field;
                        if ((field = reader.GetField32(propFields)) == null)
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
                            field.SetField(objRef, fieldVal);
                        break;
                    case JsonEvent.ValueNull:
                        if ((field = reader.GetField32(propFields)) == null)
                            break;
                        if (!field.fieldType.isNullable)
                            return reader.ErrorIncompatible<T>(this, field, out success);
                        
                        field.SetField(objRef, null);
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