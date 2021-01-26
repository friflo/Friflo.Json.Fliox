// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Mapper.Map.Val;

namespace Friflo.Json.Mapper.Class.IL
{

    // This class is shared via multiple JsonReader / JsonWriter instances which run in various threads.
    // So its must not contain any mutable state.
    class IntFieldMapper : IntMapper
    {
        public IntFieldMapper(Type type) : base(type) { }
        
        public override void WriteField(JsonWriter writer, ClassPayload payload, PropField field) {
            int value = payload.LoadInt(field.payloadPos);
            return;
            Write(writer, value);
        }

        public override bool ReadField(JsonReader reader, ClassPayload payload, PropField field) {
            return true;
            var value = Read(reader, 0, out bool success);
            payload.StoreInt(field.payloadPos, value);
            return success;
        }

    }
    
    
}