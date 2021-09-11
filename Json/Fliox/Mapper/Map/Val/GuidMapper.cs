// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Mapper.Map.Val
{
    
    internal class GuidMatcher  : ITypeMatcher {
        public static readonly GuidMatcher Instance = new GuidMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(Guid))
                return new GuidMapper (config, type);
            if (type == typeof(Guid?))
                return new NullableGuidMapper (config, type);
            return null;
        }
    }
    
    internal class GuidMapper : TypeMapper<Guid>
    {
        public  override    string          DataTypeName() { return "Guid"; }

        public GuidMapper(StoreConfig config, Type type) :
            base (config, type, false, true)
        {
        }

        public override void Write(ref Writer writer, Guid value) {
            writer.WriteGuid(value);
        }

        // ReSharper disable once RedundantAssignment
        public override Guid Read(ref Reader reader, Guid slot, out bool success) {
            ref var value = ref reader.parser.value;
            if (reader.parser.Event != JsonEvent.ValueString)
                return reader.HandleEvent(this, out success);
            if (!value.TryParseGuid(reader.charBuf, out slot, out _))
                return reader.ErrorMsg<Guid>("Failed parsing Guid. value: ", value.ToString(), out success);
            success = true;
            return slot;
        }
    }
    
    internal class NullableGuidMapper : TypeMapper<Guid?>
    {
        public override string DataTypeName() { return "Guid?"; }
        
        public NullableGuidMapper(StoreConfig config, Type type) :
            base (config, type, true, true) {
        }

        public override void Write(ref Writer writer, Guid? value) {
            if (value.HasValue)
                writer.WriteGuid(value.Value);
            else
                writer.AppendNull();
        }

        public override Guid? Read(ref Reader reader, Guid? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueString)
                return reader.HandleEvent(this, out success);
            ref var value = ref reader.parser.value;
            if (!value.TryParseGuid(reader.charBuf, out var result, out _))
                return reader.ErrorMsg<Guid?>("Failed parsing Guid. value: ", value.ToString(), out success);
            success = true;
            return result;
        }
    }
}
