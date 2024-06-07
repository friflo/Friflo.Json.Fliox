// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Mapper.Map.Val
{
    
    internal sealed class GuidMatcher  : ITypeMatcher {
        public static readonly GuidMatcher Instance = new GuidMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(Guid))
                return new GuidMapper (config, type);
            if (type == typeof(Guid?))
                return new NullableGuidMapper (config, type);
            return null;
        }
    }
    
    internal sealed class GuidMapper : TypeMapper<Guid>
    {
        public override StandardTypeId  StandardTypeId          => StandardTypeId.Guid;
        public override string          DataTypeName()          => "Guid";
        public override bool            IsNull(ref Guid value)  => false;

        public GuidMapper(StoreConfig config, Type type) :
            base (config, type, false, true)
        {
        }

        public override void Write(ref Writer writer, Guid value) {
            writer.WriteGuid(value);
        }

        // ReSharper disable once RedundantAssignment
        public override Guid Read(ref Reader reader, Guid slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueString)
                return reader.HandleEvent(this, out success);
            var value = reader.parser.value;
            if (!Bytes.TryParseGuid(value.AsSpan(), out slot))
                return reader.ErrorMsg<Guid>("Failed parsing Guid. value: ", value.AsString(), out success);
            success = true;
            return slot;
        }
    }
    
    internal sealed class NullableGuidMapper : TypeMapper<Guid?>
    {
        public override StandardTypeId  StandardTypeId          => StandardTypeId.Guid;
        public override string          DataTypeName()          => "Guid?";
        public override bool            IsNull(ref Guid? value) => !value.HasValue;
        
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
            var value = reader.parser.value;
            if (!Bytes.TryParseGuid(value.AsSpan(), out var result))
                return reader.ErrorMsg<Guid?>("Failed parsing Guid. value: ", value.AsString(), out success);
            success = true;
            return result;
        }
    }
}
