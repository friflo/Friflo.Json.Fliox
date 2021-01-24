// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Mapper.Map
{
    public class TypeNotSupportedMatcher : ITypeMatcher {
        public static readonly TypeNotSupportedMatcher Instance = new TypeNotSupportedMatcher();
        
        public ITypeMapper CreateStubType(Type type) {
            return new TypeNotSupportedMapper (type, "Type not supported. Type: " + type);
        }
    }


#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class TypeNotSupportedMapper : TypeMapper<object>
    {
        private string msg;
        
        public override string DataTypeName() { return "unsupported type"; }

        public TypeNotSupportedMapper(Type type, string msg) : base(type, true) {
            this.msg = msg;
        }

        public override object Read(JsonReader reader, object slot, out bool success) {
            throw new NotSupportedException(msg);
        }

        public override void Write(JsonWriter writer, object slot) {
            throw new NotSupportedException(msg);
        }
    }
}