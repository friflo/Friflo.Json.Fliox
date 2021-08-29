// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Flow.Mapper.Map.Val
{
    
    internal class DateTimeMatcher  : ITypeMatcher {
        public static readonly DateTimeMatcher Instance = new DateTimeMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(DateTime))
                return new DateTimeMapper (config, type);
            if (type == typeof(DateTime?))
                return new NullableDateTimeMapper (config, type);
            return null;
        }
    }
    
    internal class DateTimeMapper : TypeMapper<DateTime>
    {
        public override string DataTypeName() { return "DateTime"; }
        
        public DateTimeMapper(StoreConfig config, Type type) :
            base (config, type, false, false) {
        }

        public override void Write(ref Writer writer, DateTime value) {
            writer.WriteString(value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }

        // ReSharper disable once RedundantAssignment
        public override DateTime Read(ref Reader reader, DateTime slot, out bool success) {
            ref var value = ref reader.parser.value;
            if (reader.parser.Event != JsonEvent.ValueString)
                return reader.HandleEvent(this, out success);
            if (!DateTime.TryParse(value.ToString(), out slot))     
                return reader.ErrorMsg<DateTime>("Failed parsing DateTime. value: ", value.ToString(), out success);
            success = true;
            return slot;
        }
    }
    
    internal class NullableDateTimeMapper : TypeMapper<DateTime?>
    {
        public override string DataTypeName() { return "DateTime?"; }
        
        public NullableDateTimeMapper(StoreConfig config, Type type) :
            base (config, type, true, false) {
        }

        public override void Write(ref Writer writer, DateTime? value) {
            if (value.HasValue)
                writer.WriteString(value.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            else
                writer.AppendNull();
        }

        // ReSharper disable once RedundantAssignment
        public override DateTime? Read(ref Reader reader, DateTime? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueString)
                return reader.HandleEvent(this, out success);
            ref var value = ref reader.parser.value;
            if (!DateTime.TryParse(value.ToString(), out var result))     
                return reader.ErrorMsg<DateTime?>("Failed parsing DateTime. value: ", value.ToString(), out success);
            success = true;
            return result;
        }
    }
}
