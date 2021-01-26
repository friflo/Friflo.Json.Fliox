// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Mapper.Map.Val;

namespace Friflo.Json.Mapper.Class.IL
{

    
    /*
    abstract class FieldMapper : TypeMapper
    {
        public FieldMapper(Type type, bool isNullable) :
            base(type, isNullable)
        {
        }

        public override void            Dispose() { }
        public override void            InitTypeMapper(TypeStore typeStore) {  }
        
        public override void            WriteObject(JsonWriter writer, object slot)                     { throw new NotImplementedException(); }
        public override object          ReadObject(JsonReader reader, object slot, out bool success)    { throw new NotImplementedException(); }
        public override PropField       GetField(ref Bytes fieldName)                                   { throw new NotImplementedException(); }
        public override PropertyFields  GetPropFields()                                                 { throw new NotImplementedException(); }
        public override object          CreateInstance()                                                { throw new NotImplementedException(); }
    } */
    
    

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