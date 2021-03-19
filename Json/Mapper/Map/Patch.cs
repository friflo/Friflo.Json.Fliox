// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
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
        public string       json;
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
            writer.bytes.AppendString(value.json);
        }

        public override PatchValue Read(ref Reader reader, PatchValue slot, out bool success) {
            var stub = reader.jsonSerializerStub;
            if (stub == null)
                reader.jsonSerializerStub = stub = new JsonSerializerStub();
            
            ref var serializer = ref stub.jsonSerializer;
            serializer.InitSerializer();
            serializer.WriteTree(ref reader.parser);
            var json = serializer.json.ToString();
            var patchValue = new PatchValue { json = json };
            success = true;
            return patchValue;
        }
    }
    /*
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
            var result = base.Read(ref reader, slot, out success);
            return result;
        }
    }
    */

}