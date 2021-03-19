// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Obj;
using Friflo.Json.Mapper.Map.Obj.Reflect;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map
{
    [Fri.Discriminator("op")]
    [Fri.Polymorph(typeof(PatchReplace),    Discriminant = "replace")]
    [Fri.Polymorph(typeof(PatchAdd),        Discriminant = "add")]
    [Fri.Polymorph(typeof(PatchRemove),     Discriminant = "remove")]
    [Fri.Polymorph(typeof(PatchCopy),       Discriminant = "copy")]
    [Fri.Polymorph(typeof(PatchMove),       Discriminant = "move")]
    [Fri.Polymorph(typeof(PatchTest),       Discriminant = "test")]
    public abstract class Patch
    {
        [Fri.Ignore]
        public abstract string Path { get;  }

        public override string ToString() => Path;
    }

    public class PatchReplace : Patch
    {
        [Fri.Ignore]
        public override string Path => path;
        
        public string       path;
        public PatchValue   value;
    }
    
    public class PatchAdd : Patch
    {
        [Fri.Ignore]
        public override string Path => path;
        
        public string       path;
        public PatchValue   value;
    }
    
    public class PatchRemove : Patch
    {
        [Fri.Ignore]
        public override string Path => path;
        
        public string       path;
    }
    
    public class PatchCopy : Patch
    {
        [Fri.Ignore]
        public override string Path => path;

        public string       path;
        public string       from;
    }
    
    public class PatchMove : Patch
    {
        [Fri.Ignore]
        public override string Path => path;

        public string       path;
        public string       from;
    }
    
    public class PatchTest : Patch
    {
        [Fri.Ignore]
        public override string Path => path;

        public string       path;
        public PatchValue   value;
    }
    
    
    public class PatchValue
    {
        [Fri.Ignore]
        public object value;
        
        [Fri.Ignore]
        public TypeMapper typeMapper;
    }
    
    // ------------------------- PatchValueMatcher / PatchValueMapper -------------------------
    public class PatchValueMatcher : ITypeMatcher {
        public static readonly PatchValueMatcher Instance = new PatchValueMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type != typeof(PatchValue))
                return null;
            return new PatchValueMapper (config, type);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PatchValueMapper : TypeMapper<PatchValue>
    {
        public override string DataTypeName() { return "PatchValue"; }

        public PatchValueMapper(StoreConfig config, Type type) : base (config, type, true, false) { }

        public override void Write(ref Writer writer, PatchValue value) {
            if (value.value == null) {
                writer.AppendNull();
                return;
            }
            value.typeMapper.WriteObject(ref writer, value.value);
        }

        public override PatchValue Read(ref Reader reader, PatchValue slot, out bool success) {
            success = false;
            return default;
        }
    }
    
    // ------------------------------ PatchMatcher / PatchMapper ------------------------------
    public class PatchMatcher : ITypeMatcher {
        public static readonly PatchMatcher Instance = new PatchMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            bool isPatchType = type == typeof(Patch) || type.IsSubclassOf(typeof(Patch));
            if (!isPatchType)
                return null;
            var factory = InstanceFactory.GetInstanceFactory(type);
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            object[] constructorParams = {config, type, constructor, factory};

            // new PatchMapper (config, type, constructor, factory);
            return (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(PatchMapper<>), new[] {type}, constructorParams);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    internal class PatchMapper<TPatch> : ClassMapper<TPatch> where TPatch : Patch
    {
        public override string DataTypeName() { return "Patch"; }

        public PatchMapper(StoreConfig config, Type type, ConstructorInfo constructor, InstanceFactory factory)
            : base (config, type, constructor, factory, false)
        { }

        public override void Write(ref Writer writer, TPatch value) {
            base.Write(ref writer, value);
        }

        public override TPatch Read(ref Reader reader, TPatch slot, out bool success) {
            // Ensure preconditions are fulfilled
            if (!reader.StartObject(this, out success))
                return default;
                
            TypeMapper classType = this;
            classType = GetPolymorphType(ref reader, classType, ref slot, out success);
            if (!success)
                return default;
            TPatch objRef = slot;
            
            JsonEvent ev = reader.parser.Event;
            var fields = classType.propFields;

            while (true) {
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
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
                            return reader.ErrorIncompatible<TPatch>(this, field, out success);
                        
                        if (curFieldVal != fieldVal)
                            field.SetField(objRef, fieldVal);
                        break;
                    case JsonEvent.ValueNull:
                        if ((field = reader.GetField32(fields)) == null)
                            break;
                        if (!field.fieldType.isNullable)
                            return reader.ErrorIncompatible<TPatch>(this, field, out success);
                        
                        field.SetField(objRef, null);
                        break;

                    case JsonEvent.ObjectEnd:
                        success = true;
                        return objRef;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return reader.ErrorMsg<TPatch>("unexpected state: ", ev, out success);
                }
                ev = reader.parser.NextEvent();
            }
        }
    }


}