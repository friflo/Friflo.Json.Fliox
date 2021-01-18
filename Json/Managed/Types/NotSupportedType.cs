// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Managed.Map;

namespace Friflo.Json.Managed.Types
{
    public class NotSupportedType : StubType
    {
        public readonly string msg;
        
        public NotSupportedType(Type type, string msg) : 
            base(type, TypeNotSupportedMapper.Interface, false, TypeCat.None) {
            this.msg = msg;
        }

        public override object CreateInstance() {
            throw new NotSupportedException(msg + " " + type);
        }
        
        public override void InitStubType(TypeStore typeStore) {
        }
    }
}