// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Mapper.Map.Val
{
    
    internal sealed class DateTimeMatcher  : ITypeMatcher {
        public static readonly DateTimeMatcher Instance = new DateTimeMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(DateTime))
                return new DateTimeMapper (config, type);
            if (type == typeof(DateTime?))
                return new NullableDateTimeMapper (config, type);
            return null;
        }
    }
    
    internal sealed class DateTimeMapper : TypeMapper<DateTime>
    {
        public override string  DataTypeName()              => "DateTime";
        public override bool    IsNull(ref DateTime value)  => false;
        
        public DateTimeMapper(StoreConfig config, Type type) :
            base (config, type, false, false) {
        }

        public override void Write(ref Writer writer, DateTime value) {
            writer.WriteDateTime(value);
        }

        // ReSharper disable once RedundantAssignment
        public override DateTime Read(ref Reader reader, DateTime slot, out bool success) {
            ref var value = ref reader.parser.value;
            if (reader.parser.Event != JsonEvent.ValueString)
                return reader.HandleEvent(this, out success);
            var str = value.AsString();
            if (!DateTime.TryParse(str, out slot))
                return reader.ErrorMsg<DateTime>("Failed parsing DateTime. value: ", str, out success);
            success = true;
            return slot;
        }
        
        /// <summary>uses same format as <see cref="Bytes.AppendDateTime"/></summary>
        public static string ToRFC_3339(in DateTime value) {
            return value.ToUniversalTime().ToString(Bytes.DateTimeFormat);
        }
    }
    
    internal sealed class NullableDateTimeMapper : TypeMapper<DateTime?>
    {
        public override string  DataTypeName()              => "DateTime?";
        public override bool    IsNull(ref DateTime? value) => !value.HasValue;
        
        public NullableDateTimeMapper(StoreConfig config, Type type) :
            base (config, type, true, false) {
        }

        public override void Write(ref Writer writer, DateTime? value) {
            if (value.HasValue) {
                writer.WriteDateTime(value.Value);
            } else {
                writer.AppendNull();
            }
        }

        // ReSharper disable once RedundantAssignment
        public override DateTime? Read(ref Reader reader, DateTime? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueString)
                return reader.HandleEvent(this, out success);
            ref var value = ref reader.parser.value;
            var str = value.AsString();
            if (!DateTime.TryParse(str, out var result))     
                return reader.ErrorMsg<DateTime?>("Failed parsing DateTime. value: ", str, out success);
            success = true;
            return result;
        }
    }
}
