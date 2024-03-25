// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Object;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;

namespace Friflo.Json.Fliox.Mapper.Gen
{
    internal sealed class ClassMapperGen<T> : ClassMapper<T> {
        
        private readonly WriteDelegate<T>       write;
        private readonly ReadFieldDelegate<T>   readField;
        
        public  override bool                   IsNull(ref T value) => value == null;

        internal ClassMapperGen (
            StoreConfig             config,
            Type                    type,
            ConstructorInfo         constructor,
            InstanceFactory         instanceFactory,
            bool                    isValueType,
            Type                    genClass,
            MethodInfo              writeMethod,
            MethodInfo              readFieldMethod)
            : base (config, type, constructor, instanceFactory, isValueType, genClass)
        {
            write     = (WriteDelegate<T>)    Delegate.CreateDelegate(typeof(WriteDelegate<T>),     writeMethod);
            readField = (ReadFieldDelegate<T>)Delegate.CreateDelegate(typeof(ReadFieldDelegate<T>), readFieldMethod);
        }
        
        /// <see cref="ClassMapper{T}.Write"/>
        public override void Write(ref Writer writer, T obj) {
            int startLevel = writer.IncLevel();
            
            bool firstMember    = true;
            if (isValueType) throw new InvalidOperationException($"Expect reference type. was {type}");
            Type objType = obj.GetType();  // GetType() cost performance. May use a pre-check with isPolymorphic
            if (type != objType) {
                var classMapper = writer.typeCache.GetTypeMapper(objType);
                writer.WriteDiscriminator(this, classMapper, ref firstMember);
                classMapper.WriteObject(ref writer, obj, ref firstMember);
            } else {
                write(ref obj, propFields.fields, ref writer, ref firstMember);
            }
            writer.WriteObjectEnd(firstMember);
            writer.DecLevel(startLevel);
        }
        
        internal override void WriteObject(ref Writer writer, object value, ref bool firstMember) {
            T obj = (T)value;
            write(ref obj, propFields.fields, ref writer, ref firstMember);
        }
        
        /// <see cref="ClassMapper{T}.Read"/>
        public override T Read(ref Reader reader, T obj, out bool success)
        {
            // Ensure preconditions are fulfilled
            if (!reader.StartObject(this, out success))
                return default;
            
            var subType = GetPolymorphType(ref reader, this, ref obj, out success);
            if (!success)
                return default;
            if (subType != null) {
                return (T)subType.ReadObject(ref reader, obj, out success);
            }
            var         ev      = reader.parser.Event;
            Span<bool>  found   = reader.setMissingFields ? stackalloc bool [GetFoundCount()] : default;

            while (true) {
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ValueNull:
                        PropField field;
                        if ((field = reader.GetField32(propFields)) == null)
                            break;
                        if (reader.setMissingFields) found[field.fieldIndex] = true;
                        success = readField(ref obj, field, ref reader);
                        if (!success)
                            return default;
                        break;
                    case JsonEvent.ObjectEnd:
                        if (reader.setMissingFields) ClearReadToFields(obj, found);
                        success = true;
                        return obj;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return reader.ErrorMsg<T>("unexpected state: ", ev, out success);
                }
                ev = reader.parser.NextEvent();
            }
        }
        
        internal override object ReadObject(ref Reader reader, object value, out bool success) {
            var         obj     = (T)value;
            var         ev      = reader.parser.Event;
            Span<bool>  found   = reader.setMissingFields ? stackalloc bool [GetFoundCount()] : default;
            while (true) {
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ValueNull:
                        PropField field;
                        if ((field = reader.GetField32(propFields)) == null)
                            break;
                        if (reader.setMissingFields) found[field.fieldIndex] = true;
                        success = readField(ref obj, field, ref reader);
                        if (!success)
                            return default;
                        break;
                    case JsonEvent.ObjectEnd:
                        if (reader.setMissingFields) ClearReadToFields(obj, found);
                        success = true;
                        return obj;
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