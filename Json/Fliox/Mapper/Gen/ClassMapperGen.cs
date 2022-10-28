// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Object;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;

namespace Friflo.Json.Fliox.Mapper.Gen
{
    internal class ClassMapperGen<T> : ClassMapper<T> {
        
        private readonly WriteDelegate<T>        write;
        private readonly ReadFieldDelegate<T>    readField;

        protected ClassMapperGen (
            StoreConfig             config,
            Type                    type,
            ConstructorInfo         constructor,
            InstanceFactory         instanceFactory,
            bool                    isValueType,
            MethodInfo              writeMethod,
            MethodInfo              readFieldMethod)
            : base (config, type, constructor, instanceFactory, isValueType)
        {
            write     = (WriteDelegate<T>)    Delegate.CreateDelegate(typeof(WriteDelegate<T>),     writeMethod);
            readField = (ReadFieldDelegate<T>)Delegate.CreateDelegate(typeof(ReadFieldDelegate<T>), readFieldMethod);
        }
        
        public override void Write(ref Writer writer, T obj) {
            int startLevel = writer.IncLevel();
            bool firstMember = true;
            
            write(obj, propFields.fields, ref writer, ref firstMember);
            
            writer.WriteObjectEnd(firstMember);
            writer.DecLevel(startLevel);
        }
        
        // T Read(T obj, PropField[] fields, ref Reader reader, out bool success)
        public override T Read(ref Reader reader, T obj, out bool success)
        {
            // Ensure preconditions are fulfilled
            if (!reader.StartObject(this, out success))
                return default;
            var ev = reader.parser.NextEvent();

            while (true) {
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ValueNull:
                        PropField field;
                        if ((field = reader.GetField(propFields)) == null)
                            break;
                        success = readField(obj, field, ref reader);
                        if (!success)
                            return default;
                        break;
                    case JsonEvent.ObjectEnd:
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