// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Managed.Codecs;

namespace Friflo.Json.Managed.Types
{
    public class NotSupportedType : StubType {
        public NotSupportedType(Type type) : 
            base(type, TypeNotSupportedCodec.Interface) {
        }
        
        public override void InitStubType(TypeResolver resolver) {
        }


        public override object CreateInstance() {
            throw new NotSupportedException("Type not supported" + type.FullName);
        }
    }
}