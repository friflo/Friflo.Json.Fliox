// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map;

namespace Friflo.Json.Mapper.Types
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class NotSupportedType : StubType
    {
        public readonly string msg;
        
        public NotSupportedType(Type type, string msg) : 
            base(type, TypeNotSupportedMapper.Interface, false) {
            this.msg = msg;
        }

        public override object CreateInstance() {
            throw new NotSupportedException(msg + " " + type);
        }
        
        public override void InitStubType(TypeStore typeStore) {
        }
    }
}