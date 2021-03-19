// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

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
        public string path;

        public override string ToString() => path;
    }

    public class PatchReplace : Patch
    {
        public PatchValue value;
    }
    
    public class PatchAdd : Patch
    {
        public PatchValue value;
    }
    
    public class PatchRemove : Patch
    {
    }
    
    public class PatchCopy : Patch
    {
        public string from;
    }
    
    public class PatchMove : Patch
    {
        public string from;
    }
    
    public class PatchTest : Patch
    {
        public PatchValue value;
    }
    
    
    public class PatchValue
    {
        [Fri.Ignore]
        public object value;
        
        [Fri.Ignore]
        public TypeMapper typeMapper;
    }
    
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


}