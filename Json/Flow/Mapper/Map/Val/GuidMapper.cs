// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Flow.Mapper.Map.Val
{
    
    public class GuidMatcher  : ITypeMatcher {
        public static readonly GuidMatcher Instance = new GuidMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(Guid))
                return new GuidMapper (config, type);
            if (type == typeof(Guid?))
                return new NullableGuidMapper (config, type);
            return null;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class GuidMapper : TypeMapper<Guid>
    {
        public  override    string          DataTypeName() { return "Guid"; }
        public  override    TypeMapper      GetUnderlyingMapper() => stringMapper; // todo - remove?
        private readonly    TypeMapper      stringMapper;

        public GuidMapper(StoreConfig config, Type type) :
            base (config, type, false, false)
        {
            stringMapper = StringMatcher.Instance.MatchTypeMapper(typeof(string), config);
        }

        public override void Write(ref Writer writer, Guid value) {
            writer.WriteString(value.ToString());
        }

        // ReSharper disable once RedundantAssignment
        public override Guid Read(ref Reader reader, Guid slot, out bool success) {
            ref var value = ref reader.parser.value;
            if (reader.parser.Event != JsonEvent.ValueString)
                return reader.HandleEvent(this, out success);
            if (!Guid.TryParse(value.ToString(), out slot))     
                return reader.ErrorMsg<Guid>("Failed parsing Guid. value: ", value.ToString(), out success);
            success = true;
            return slot;
        }
    }
    
    public class NullableGuidMapper : TypeMapper<Guid?>
    {
        public override string DataTypeName() { return "Guid?"; }
        
        public NullableGuidMapper(StoreConfig config, Type type) :
            base (config, type, true, false) {
        }

        public override void Write(ref Writer writer, Guid? value) {
            if (value.HasValue)
                writer.WriteString(value.Value.ToString());
            else
                writer.AppendNull();
        }

        public override Guid? Read(ref Reader reader, Guid? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueString)
                return reader.HandleEvent(this, out success);
            ref var value = ref reader.parser.value;
            if (!Guid.TryParse(value.ToString(), out var dateTime))     
                return reader.ErrorMsg<Guid?>("Failed parsing Guid. value: ", value.ToString(), out success);
            success = true;
            return dateTime;
        }
    }
}
